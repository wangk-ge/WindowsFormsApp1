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
        private delegate void SerialDataRecved(List<WaveDecoder.WaveData> dataList);

        public Form1()
        {
            InitializeComponent();
        }

        private void OnDataRecved(List<WaveDecoder.WaveData> dataList)
        {
            foreach(WaveDecoder.WaveData data in dataList)
            {
                Console.WriteLine(data.data.value * 1000);
                this.chart1.Series[0].Points.AddXY(m_x++, data.data.value * 1000);
            }
        }

        public void RecvData(List<WaveDecoder.WaveData> dataList)
        {
            this.BeginInvoke(new SerialDataRecved(OnDataRecved), dataList);
        }
    }
}
