using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;
using System.Collections.Concurrent;

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
            FlowSensor sensor = new FlowSensor();
            sensor.Open("COM10");
            Application.Run(mainForm);
        }
    }

    /* 流速传感器 */
    class FlowSensor
    {
        public enum FrameType { CmdResp, DataPoint };
        public delegate void FrameRecvHandler(FrameType type, object frameData);
        public event FrameRecvHandler FrameRecved;

        private SerialPort m_serialPort = null;
        private WaveDecoder m_waveDecoder = new WaveDecoder();
        private SerialDataReceivedEventHandler m_serialDataRecvHandler;
        private string m_currentCmdResp = null;

        public FlowSensor()
        {
            m_waveDecoder.Test();
        }

        public void Open(string portName)
        {
            m_serialPort.Close();
            m_serialPort = new SerialPort(portName);
            m_serialPort.BaudRate = 115200;
            m_serialPort.Parity = Parity.None;
            m_serialPort.StopBits = StopBits.One;
            m_serialPort.DataBits = 8;
            m_serialPort.Handshake = Handshake.None;
            m_serialPort.RtsEnable = true;

            m_serialDataRecvHandler = new SerialDataReceivedEventHandler(DataReceivedHandler);
            m_serialPort.DataReceived += m_serialDataRecvHandler;

            m_serialPort.Open();
        }

        public void Close()
        {
            m_serialPort.Close();
            m_serialPort = null;
        }

        public async Task<string> ExcuteCmdAysnc(string cmd, int timeout)
        {
            m_serialPort.Write(cmd);

            string cmdResp = await Task.Run(() => {
                string result = "";
                int timeoutCnt = (timeout > 0) ? timeout : 0;
                lock (m_currentCmdResp)
                {
                    if (timeoutCnt > 0)
                    {
                        while ((null == m_currentCmdResp) && (timeoutCnt > 0))
                        {
                            Thread.Sleep(1);
                            --timeoutCnt;
                        }
                    }
                    else
                    {
                        while (null == m_currentCmdResp)
                        {
                            Thread.Sleep(1);
                        }
                    }
                    result = m_currentCmdResp;
                    m_currentCmdResp = null;
                }
                return result;
            });

            return cmdResp;
        }

        private void DataReceivedHandler(
                        object sender,
                        SerialDataReceivedEventArgs e)
        {
            SerialPort sp = sender as SerialPort;
            int dataLen = sp.BytesToRead;
            byte[] dataBuf = new byte[dataLen];
            sp.Read(dataBuf, 0, dataLen);
            List<WaveDecoder.WaveData> dataList = m_waveDecoder.FrameDecode(dataBuf);
            // TODO
            lock(m_currentCmdResp)
            {
                if (null == m_currentCmdResp)
                {
                    m_currentCmdResp = "";
                }
            }
            // TODO
        }
    }
}
