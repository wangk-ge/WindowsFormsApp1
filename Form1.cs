using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private delegate void SerialDataRecved(byte[] data);

        public Form1()
        {
            InitializeComponent();
        }

        private void OnDataRecved(byte[] data)
        {
            Console.WriteLine(data);
        }

        public void RecvData(byte[] data)
        {
            this.BeginInvoke(new SerialDataRecved(OnDataRecved), data);
        }
    }
}
