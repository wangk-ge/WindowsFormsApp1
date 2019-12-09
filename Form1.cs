using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private int m_x = 0;
        private FlowSensor m_flowSensor = new FlowSensor();
        private delegate void WaveDataRecved(byte channel, double value);
        private delegate void CmdRespRecved(string cmdResp);

        public Form1()
        {
            InitializeComponent();

            m_flowSensor.m_frameDecoder.WaveDataRespRecved += new FrameDecoder.WaveDataRecvHandler((byte channel, double value) => {
                Console.WriteLine($"WaveDataRespRecved: {channel} {value}");
                this.BeginInvoke(new WaveDataRecved(OnWaveDataRecved), channel, value);
            });
            m_flowSensor.m_frameDecoder.CmdRespRecved += new FrameDecoder.CmdRespRecvHandler((string cmdResp) => {
                Console.WriteLine($"CmdRespRecved: {cmdResp}");
                this.BeginInvoke(new CmdRespRecved(OnCmdRespRecved), cmdResp);
            });
            m_flowSensor.m_frameDecoder.TimeStampRespRecved += new FrameDecoder.TimeStampRecvHandler((FrameDecoder.TimeStamp timeStamp) => {
                Console.WriteLine($"TimeStampRespRecved: {timeStamp}");
            });

            m_flowSensor.Open("COM4");
        }

        private void OnWaveDataRecved(byte channel, double value)
        {
            this.chart1.Series[0].Points.AddXY(m_x++, value * 1000);
        }

        private void OnCmdRespRecved(string cmdResp)
        {
            this.cmdRespTextBox.AppendText($"Revc: {cmdResp} \r\n");
        }

        private void sendCmdButton_Click(object sender, EventArgs e)
        {
            m_flowSensor.ExcuteCmd(this.cmdTextBox.Text);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
