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
            Application.Run(mainForm);
        }
    }

    /* 流速传感器 */
    class FlowSensor
    {
        private SerialPort m_serialPort = null;

        public FrameDecoder m_frameDecoder = new FrameDecoder();
        private Queue<TaskCompletionSource<string>> m_cmdRespTaskCompQue = new Queue<TaskCompletionSource<string>>();

        public FlowSensor()
        {
            FrameDecoder.Test();
        }

        public void Open(string portName)
        {
            m_serialPort?.Close();
            m_serialPort = new SerialPort(portName);
            m_serialPort.BaudRate = 115200;
            m_serialPort.Parity = Parity.None;
            m_serialPort.StopBits = StopBits.One;
            m_serialPort.DataBits = 8;
            m_serialPort.Handshake = Handshake.None;
            m_serialPort.RtsEnable = true;

            m_serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

            m_frameDecoder.CmdRespRecved += new FrameDecoder.CmdRespRecvHandler((string cmdResp) => {
                Console.WriteLine($"CmdRespRecved: {cmdResp}");
                TaskCompletionSource<string> taskComp = null;
                lock (m_cmdRespTaskCompQue)
                {
                    taskComp = m_cmdRespTaskCompQue.Dequeue();
                }
                taskComp.SetResult(cmdResp);
            });

            m_serialPort.Open();
        }

        public void Close()
        {
            m_serialPort.Close();
            m_serialPort = null;
        }

        private Task<string> ExcuteCmdTask(string cmd)
        {
            var cmdRespTaskComp = new TaskCompletionSource<string>();
            lock (m_cmdRespTaskCompQue)
            {
                m_cmdRespTaskCompQue.Enqueue(cmdRespTaskComp);
            }

            m_serialPort.Write(cmd);

            var task = cmdRespTaskComp.Task;
            return task;
        }

        /* 执行命令(异步版本) */
        public async Task<string> ExcuteCmdAsync(string cmd, int timeOut)
        {
            var cmdTask = ExcuteCmdTask(cmd);
            var task = await Task.WhenAny(cmdTask, Task.Delay(timeOut));
            if (task == cmdTask)
            { // 成功
                return cmdTask.Result;
            }

            // 超时
            lock (m_cmdRespTaskCompQue)
            {
                m_cmdRespTaskCompQue.Dequeue();
            }
            return "";
        }

        /* 执行命令(同步版本) */
        public string ExcuteCmd(string cmd, int timeOut)
        {
            var cmdTask = ExcuteCmdTask(cmd);
            var compTask = Task.WhenAny(cmdTask, Task.Delay(timeOut));
            var task = compTask.Result;
            if (task == cmdTask)
            {
                return cmdTask.Result;
            }

            // 超时
            lock (m_cmdRespTaskCompQue)
            {
                m_cmdRespTaskCompQue.Dequeue();
            }
            return "";
        }

        private void DataReceivedHandler(
                        object sender,
                        SerialDataReceivedEventArgs e)
        {
            SerialPort sp = sender as SerialPort;
            int dataLen = sp.BytesToRead;
            byte[] dataBuf = new byte[dataLen];
            sp.Read(dataBuf, 0, dataLen);
            m_frameDecoder.FrameDecode(dataBuf);
        }
    }
}
