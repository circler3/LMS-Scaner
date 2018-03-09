using System;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace Walker
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        System.Timers.Timer timer = new System.Timers.Timer();
        private string dataFolder;

        private void BtnFullScan_Click(object sender, EventArgs e)
        {
            //Modbus.Program.CaptureSinglePhotoAsync("d:\\");
            Modbus.Program.Capture5PhotosAsync("d:\\");
            //Init();
            //timer.Elapsed += Timer_Elapsed;
            //timer.Interval = 5000;
            //timer.AutoReset = true;
            //timer.Start();
        }

        public void Init()
        {
            dataFolder = LMS111.Program.Init();
        }

        private async void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            //扫描仪数据采集
            await LMS111.Program.SendRequestAsync(dataFolder);
            //5个图像采集
            await Modbus.Program.Capture5PhotosAsync(dataFolder);
            //单独图像拍摄
            await Modbus.Program.CaptureSinglePhotoAsync(dataFolder);
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


        private async void btnPLCSet_Click(object sender, EventArgs e)
        {
            try
            {
                await Modbus.Program.SendPLC(GetUShort(tbPortNum.Text), GetUShort(tbRegisterAddress.Text), Convert.ToInt32(tbPulseCount.Text));
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }

        private ushort GetUShort(string text)
        {
            //if (Regex.IsMatch(text, @"/^\d+$/ "))
            //{
            //    return Convert.ToUInt16(text);
            //}
            //else if (Regex.IsMatch(text, @"^0x[a-f0-9]{1,2}$)|(^0X[A-F0-9]{1,2}$)|(^[A-F0-9]{1,2}$)|(^[a-f0-9]{1,2}$"))
            //{
            //    return Convert.ToUInt16(text, 16);
            //}
            //else
            //{
            //    throw new FormatException("输入数值不合法");
            //}
            return Convert.ToUInt16(text);
        }
    }
}
