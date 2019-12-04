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
            dataSource.Open("COM10");
            Application.Run(mainForm);
        }
    }

    class DataSource
    {
        private SerialPort m_serialPort = null;
        private Form1 m_mainForm = null;

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
            m_mainForm.RecvData(dataBuf);
        }
    }

    class WaveDecoder
    {
        uint m_data;
        int m_type;
        char m_channel;
        int m_status;
        int m_frameCount, m_dataCount;
        int m_frameLength, m_dataLength;
        byte[] m_infoFrame;
    }
}
