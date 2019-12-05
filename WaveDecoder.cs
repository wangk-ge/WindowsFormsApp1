using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    /* Wave格式数据流解析器 */
    public class WaveDecoder
    {
        public enum DataMode
        {
            ValueMode,      // 数值
            TimeStampMode   // 时间戳
        }

        public struct TimeStamp
        {
            public byte year;
            public byte month;
            public byte day;
            public byte hour;
            public byte min;
            public byte sec;
            public ushort msec;
            public uint sampleRate;

            override public string ToString()
            {
                return string.Format("{0:D2}-{1:D2}-{2:D2} {3:D2}:{4:D2}:{5:D2}:{6:D4} {7:D4}", 
                    year, month, day, hour, min, sec, msec, sampleRate);
            }
        }

        public struct DataType
        {
            public DataMode mode;
            public byte channel;
            [StructLayout(LayoutKind.Explicit, Size = 4)]
            public struct Data
            { // 模拟实现union
                [FieldOffset(0)]
                public TimeStamp ts;
                [FieldOffset(0)]
                public double value;
            }
            public Data data;

            override public string ToString()
            {
                if (DataMode.ValueMode == mode)
                {
                    return string.Format("{0:G}: {1:G}", channel, data.value);
                }
                else // if (DataMode.TimeStampMode == mode)
                {
                    return string.Format("{0:G}", data.ts);
                }
            }
        }

        private enum FrameFlag
        {
            Frame_Head = 0xA3,        // 帧头识别字
            Frame_PointMode = 0xA8,   // 点模式识别字
            Frame_SyncMode = 0xA9,    // 同步模式识别字
            Frame_InfoMode = 0xAA     // 信息帧识别字
        }

        private enum Format
        {
            Format_Float = 0x00,    // float识别字
            Format_Int8 = 0x10,     // int8识别字
            Format_Int16 = 0x20,    // int16识别字
            Format_Int32 = 0x30     // int32识别字
        }

        private enum Status
        {
            STA_None = 0, // 空闲状态
            STA_Head,     // 接收到帧头
            STA_Point,    // 点模式
            STA_Sync,     // 同步模式
            STA_Info,     // 信息模式
            STA_SyncData  // 同步模式数据
        }

        private enum Result
        {
            Ok = 0,
            Error,
            Done,
        }

        private uint m_data = 0;
        private Format m_format = Format.Format_Float;
        private byte m_channel = 0;
        private Status m_status = Status.STA_None;
        private int m_frameCount = 0, m_dataCount = 0;
        private int m_frameLength = 0, m_dataLength = 0;
        private byte[] m_infoFrame = new byte[8];
        /* 各种类型的字节数 0: float, 1: int8, 2: int16, 3: int32 */
        private static readonly int[] s_fmtBytes = { 4, 1, 2, 4 };

        public WaveDecoder()
        {
            // do nothing
        }

        /* 数据转换为double浮点数 */
        private double DataToDouble(uint value, Format format)
        {
            double d = 0.0;

            switch (format)
            {
                case Format.Format_Float: // float
                    unsafe
                    {
                        byte* ptr = (byte*)&value;
                        float fval = *((float*)ptr);
                        d = fval;
                    }
                    break;
                case Format.Format_Int8: // int8
                    d = (sbyte)value;
                    break;
                case Format.Format_Int16: // int16
                    d = (short)value;
                    break;
                case Format.Format_Int32: // int32
                    d = (int)value;
                    break;
                default:
                    d = 0.0;
                    break;
            }

            return d;
        }

        // 接收一个点数据, 仅仅是数据
        private Result ParsePointData(ref DataType data, byte bData)
        {
            if (m_dataCount == 0)
            { // 第一个字节是数据类型和通道信息
                m_channel = (byte)(bData & 0x0F); // 通道值
                m_format = (Format)(bData & 0xF0);
                // 0: float, 1: int8, 2: int16, 3: int32
                int fmtIndex = ((byte)m_format >> 4);
                if (fmtIndex > 3)
                { // 数据类型错误
                    m_dataCount = 0;
                    return Result.Error;
                }
                m_dataLength = s_fmtBytes[fmtIndex];
            }
            else
            { // 后面几个字节是数据
                m_data = (m_data << 8) | bData;
                if (m_dataCount >= m_dataLength)
                { // 接收完毕
                    data.channel = m_channel;
                    data.mode = DataMode.ValueMode;
                    data.data.value = DataToDouble(m_data, m_format);
                    m_dataCount = 0;
                    m_data = 0;
                    return Result.Done;
                }
            }
            ++m_dataCount;
            return Result.Ok;
        }

        // 转换时间戳
        private void ParseTimeStamp(ref DataType data, byte[] buffer)
        {
            ref TimeStamp ts = ref data.data.ts;

            data.mode = DataMode.TimeStampMode;
            ts.year = (byte)((buffer[0] >> 1) & 0x7F);
            ts.month = (byte)(((buffer[0] << 3) & 0x80) | ((buffer[1] >> 5) & 0x07));
            ts.day = (byte)(buffer[1] & 0x1F);
            ts.hour = (byte)((buffer[2] >> 3) & 0x1F);
            ts.min = (byte)(((buffer[2] << 3) & 0x38) | ((buffer[3] >> 5) & 0x07));
            ts.sec = (byte)(((buffer[3] << 1) & 0x3E) | ((buffer[4] >> 7) & 0x01));
            ts.msec = (ushort)((((ushort)buffer[4] << 3) & 0x03F8) | (((ushort)buffer[5] >> 5) & 0x0007));
            ts.sampleRate = (((uint)buffer[5] << 16) & 0x1F0000)
                | (((uint)buffer[6] << 8) & 0x00FF00) | (uint)buffer[7];
        }

        /* 逐字节输入数据进行解码 */
        private bool FrameDecodeInput(ref DataType tData, byte byteData)
        {
            Result res = Result.Ok;

            // 捕获帧头状态机
            switch (m_status)
            {
                case Status.STA_None:
                    m_status = ((FrameFlag)byteData == FrameFlag.Frame_Head) ? Status.STA_Head : Status.STA_None;
                    break;
                case Status.STA_Head:
                    /* byteData == Frame_PointMode -> m_status = STA_Point
                     * byteData == Frame_SyncMode -> m_status = STA_Sync
                     * byteData == Frame_InfoMode -> m_status = STA_Info
                     * else -> m_status = STA_None
                     */
                    switch ((FrameFlag)byteData)
                    {
                        case FrameFlag.Frame_PointMode:
                            m_status = Status.STA_Point;
                            break;
                        case FrameFlag.Frame_SyncMode:
                            m_status = Status.STA_Sync;
                            break;
                        case FrameFlag.Frame_InfoMode:
                            m_status = Status.STA_Info;
                            m_frameCount = 0;
                            break;
                        default:
                            m_status = Status.STA_None;
                            break;
                    }

                    break;
                case Status.STA_Point:
                    res = ParsePointData(ref tData, byteData);
                    switch (res)
                    {
                        case Result.Ok: // 还在接收数据
                            break;
                        case Result.Error: // 错误则重新开始接收
                            m_status = Status.STA_None;
                            break;
                        case Result.Done: // 结束初始化状态并返回true
                            m_status = Status.STA_None;
                            return true;
                    }
                    break;
                case Status.STA_Sync:
                    m_frameCount = 0;
                    m_frameLength = byteData;
                    // 如果len > 80则帧长度错误, 将重新匹配帧, 否则转到STA_SyncData状态
                    m_status = (m_frameLength <= 80) ? Status.STA_SyncData : Status.STA_None;
                    break;
                case Status.STA_SyncData:
                    if (++m_frameCount >= m_frameLength)
                    { // 计数达到帧长度说明帧结束, 重置状态
                        m_status = Status.STA_None;
                    }
                    res = ParsePointData(ref tData, byteData);
                    switch (res)
                    {
                        case Result.Ok: // 还在接收数据
                            break;
                        case Result.Error: // 错误则重新开始接收
                            m_status = Status.STA_None;
                            break;
                        case Result.Done: // 结束返回true
                            return true;
                    }
                    break;
                case Status.STA_Info:
                    m_infoFrame[m_frameCount++] = byteData;
                    if (m_frameCount >= 8)
                    {
                        ParseTimeStamp(ref tData, m_infoFrame);
                        m_frameCount = 0;
                        m_status = Status.STA_None;
                        return true;
                    }
                    break;
                default: // 异常情况复位状态
                    m_status = Status.STA_None;
                    break;
            }
            return false;
        }

        /* 解码字节流,返回解码结果 */
        public List<DataType> FrameDecode(byte[] byteArray)
        {
            DataType data = new DataType();
            List<DataType> list = new List<DataType>();

            foreach (byte bData in byteArray)
            {
                if (FrameDecodeInput(ref data, bData) == true)
                {
                    list.Add(data);
                }
            }
            return list;
        }

        /* 单元测试 */
        public void Test()
        {
            byte[] frame = {
                /* int8 */
                0xa3,0xa8,0x10,0xfc, /* -4 */
                0xa3,0xa8,0x10,0xfd, /* -3 */
                0xa3,0xa8,0x10,0xfe, /* -2 */
                0xa3,0xa8,0x10,0xff, /* -1 */
                0xa3,0xa8,0x10,0x00, /* 0 */
                0xa3,0xa8,0x10,0x01, /* 1 */
                0xa3,0xa8,0x10,0x02, /* 2 */
                0xa3,0xa8,0x10,0x03, /* 3 */
                0xa3,0xa8,0x10,0x04, /* 4 */
                /* int16 */
                0xa3,0xa8,0x21,0x80,0x01, /* -32767 */
                0xa3,0xa8,0x21,0xff,0xfe, /* -2 */
                0xa3,0xa8,0x21,0xff,0xff, /* -1 */
                0xa3,0xa8,0x21,0x00,0x00, /* 0 */
                0xa3,0xa8,0x21,0x00,0x01, /* 1 */
                0xa3,0xa8,0x21,0x00,0x02, /* 2 */
                0xa3,0xa8,0x21,0x7f,0xff, /* 32767 */
                /* int32 */
                0xa3,0xa8,0x32,0xff,0xfb,0x00,0x0a, /* -327670 */
                0xa3,0xa8,0x32,0xff,0xff,0xff,0xfe, /* -2 */
                0xa3,0xa8,0x32,0xff,0xff,0xff,0xff, /* -1 */
                0xa3,0xa8,0x32,0x00,0x00,0x00,0x00, /* 0 */
                0xa3,0xa8,0x32,0x00,0x00,0x00,0x01, /* 1 */
                0xa3,0xa8,0x32,0x00,0x00,0x00,0x02, /* 2 */
                0xa3,0xa8,0x32,0x00,0x04,0xff,0xf6, /* 327670 */
                /* float */
                0xa3,0xa8,0x03,0xc0,0x80,0xa3,0xd7, /* -4.020000 */
                0xa3,0xa8,0x03,0xc0,0x40,0xa3,0xd7, /* -3.010000 */
                0xa3,0xa8,0x03,0xc0,0x00,0x00,0x00, /* -2.000000 */
                0xa3,0xa8,0x03,0xbf,0x80,0x00,0x00, /* -1.000000 */
                0xa3,0xa8,0x03,0x00,0x00,0x00,0x00, /* 0.000000 */
                0xa3,0xa8,0x03,0x3f,0x80,0x00,0x00, /* 1.000000 */
                0xa3,0xa8,0x03,0x40,0x00,0x00,0x00, /* 2.000000 */
                0xa3,0xa8,0x03,0x40,0x41,0xeb,0x85, /* 3.030000 */
                0xa3,0xa8,0x03,0x40,0x81,0x47,0xae, /* 4.040000 */
                /* timestamp */
                0xa3,0xaa,0x27,0x85,0x5c,0x8a,0x19,0x00,0x00,0x96, /* 19-12-05 11:36:20: 0200 0150 */
            };

            List<DataType> list = FrameDecode(frame);

            foreach(DataType data in list)
            {
                Console.WriteLine(data);
            }
        }
    }
}
