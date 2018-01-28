using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Walker
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        System.Timers.Timer timer = new System.Timers.Timer();

        private void BtnFullScan_Click(object sender, EventArgs e)
        {
            Init();
            timer.Elapsed += Timer_Elapsed;
            timer.Interval = 5000;
            timer.AutoReset = true;
            timer.Start();
        }

        public void Init()
        {
            LMS111.Program.Init();
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            LMS111.Program.SendRequest();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            //停止大循环定时器
            timer.Stop();
            //停止511写入文件数据循环
            LMS111.Program.Stop();
            //停止5个图像采集

            //停止单独图像拍摄

        }
    }
}
