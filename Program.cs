using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;

namespace WindowsFormsApp1
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Form1 mainForm = new Form1();
            DataSource dataSource = new DataSource(mainForm);
            dataSource.Open("COM4");
            Application.Run(mainForm);
        }
    }

    class DataSource
    {
        private SerialPort m_serialPort = null;
        private Form1 m_mainForm = null;
        private WaveDecoder m_waveDecoder = new WaveDecoder();

        public DataSource(Form1 mainForm)
        {
            m_mainForm = mainForm;
        }

        public void Open(string portName)
        {
            if (null != m_serialPort)
            {
                m_serialPort.Close();
            }
            m_serialPort = new SerialPort(portName);
            m_serialPort.BaudRate = 115200;
            m_serialPort.Parity = Parity.None;
            m_serialPort.StopBits = StopBits.One;
            m_serialPort.DataBits = 8;
            m_serialPort.Handshake = Handshake.None;
            m_serialPort.RtsEnable = true;

            m_serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

            m_serialPort.Open();
        }

        public void Close()
        {
            if (null != m_serialPort)
            {
                m_serialPort.Close();
                m_serialPort = null;
            }
        }

        private void DataReceivedHandler(
                        object sender,
                        SerialDataReceivedEventArgs e)
        {
            SerialPort sp = sender as SerialPort;
            int dataLen = sp.BytesToRead;
            byte[] dataBuf = new byte[dataLen];
            sp.Read(dataBuf, 0, dataLen);
            List<WaveDecoder.DataType> dataList = m_waveDecoder.frameDecode(dataBuf);
            m_mainForm.RecvData(dataList);
        }
    }

    public class WaveDecoder
    {
        public enum DataMode
        {
            ValueMode,
            TimeStampMode
        };
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
        };
        public struct DataType
        {
            public DataMode mode;
            public byte channel;
            public TimeStamp ts;
            public double value;
        };

        private enum Mode
        {
            Frame_Head = 0xA3,          //帧头识别字
            Frame_PointMode = 0xA8,     // 点模式识别字
            Frame_SyncMode = 0xA9,      // 同步模式识别字
            Frame_InfoMode = 0xAA      // 信息帧识别字
        };

        private enum Status
        {
            STA_None = 0, // 空闲状态
            STA_Head,     // 接收到帧头
            STA_Point,    // 点模式
            STA_Sync,     // 同步模式
            STA_Info,     // 信息模式
            STA_SyncData  // 同步模式数据
        };

        private enum Result
        {
            Ok = 0,
            Error,
            Done,
        };

        private uint m_data;
        private int m_type;
        private byte m_channel;
        private Status m_status;
        private int m_frameCount, m_dataCount;
        private int m_frameLength, m_dataLength;
        private byte[] m_infoFrame;

        public WaveDecoder()
        {
            m_status = Status.STA_None;
            m_dataCount = 0;
            m_frameCount = 0;
            m_dataLength = 0;
            m_frameLength = 0;
        }

        private double data2Double(uint value, int type)
        {
            double d = 0.0;

            switch (type)
            {
                case 0: // float
                    unsafe
                    {
                        byte* ptr = (byte*)&value;
                        float fval = *((float*)ptr);
                        d = fval;
                    }
                    break;
                case 1: // int8
                    d = (sbyte)value;
                    break;
                case 2: // int16
                    d = (short)value;
                    break;
                case 3: // int32
                    d = (int)value;
                    break;
                default:
                    d = 0.0;
                    break;
            }
            return d;
        }

        // 接收一个点数据, 仅仅是数据
        private Result pointData(ref DataType data, byte bData)
        {
            int[] bytes = { 4, 1, 2, 4 }; // 各种类型的字节数

            if (m_dataCount == 0)
            { // 第一个字节是数据类型和通道信息
                m_channel = (byte)(bData & 0x0F); // 通道值
                // m_type: 0: float, 1: int8, 2: int16, 3: int32
                m_type = bData >> 4;
                if (m_type > 3)
                { // 数据类型错误
                    m_dataCount = 0;
                    return Result.Error;
                }
                m_dataLength = bytes[m_type];
            }
            else
            { // 后面几个字节是数据
                m_data = (m_data << 8) | bData;
                if (m_dataCount >= m_dataLength)
                { // 接收完毕
                    data.channel = m_channel;
                    data.mode = DataMode.ValueMode;
                    data.value = data2Double(m_data, m_type);
                    m_dataCount = 0;
                    m_data = 0;
                    return Result.Done;
                }
            }
            ++m_dataCount;
            return Result.Ok;
        }

        // 转换时间戳
        private void timeStamp(ref DataType data, byte[] buffer)
        {
            ref TimeStamp ts = ref data.ts;

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

        // 波形数据帧解码, 会识别帧头
        private bool frameDecode_p(ref DataType data, byte bData)
        {
            Result res = Result.Ok;

            // 捕获帧头状态机
            switch (m_status)
            {
                case Status.STA_None:
                    m_status = ((Mode)bData == Mode.Frame_Head) ? Status.STA_Head : Status.STA_None;
                    break;
                case Status.STA_Head:
                    /* byte == Frame_PointMode -> m_status = STA_Point
                     * byte == Frame_SyncMode -> m_status = STA_Sync
                     * byte == Frame_InfoMode -> m_status = STA_Info
                     * else -> m_status = STA_None
                     */
                    switch ((Mode)bData) {
                        case Mode.Frame_PointMode:
                            m_status = Status.STA_Point;
                            break;
                        case Mode.Frame_SyncMode:
                            m_status = Status.STA_Sync;
                            break;
                        case Mode.Frame_InfoMode:
                            m_status = Status.STA_Info;
                            m_frameCount = 0;
                            break;
                        default:
                            m_status = Status.STA_None;
                            break;
                    }

                    break;
                case Status.STA_Point:
                    res = pointData(ref data, bData);
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
                    m_frameLength = bData;
                    // 如果len > 80则帧长度错误, 将重新匹配帧, 否则转到STA_SyncData状态
                    m_status = m_frameLength <= 80 ? Status.STA_SyncData : Status.STA_None;
                    break;
                case Status.STA_SyncData:
                    if (++m_frameCount >= m_frameLength)
                    { // 计数达到帧长度说明帧结束, 重置状态
                        m_status = Status.STA_None;
                    }
                    res = pointData(ref data, bData);
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
                    m_infoFrame[m_frameCount++] = bData;
                    if (m_frameCount >= 8)
                    {
                        timeStamp(ref data, m_infoFrame);
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

        public List<DataType> frameDecode(byte[] byteArray)
        {
            DataType data = new DataType();
            List<DataType> list = new List<DataType>();

            foreach (byte bData in byteArray)
            {
                if (frameDecode_p(ref data, bData) == true)
                {
                    list.Add(data);
                }
            }
            return list;
        }
    }
}
