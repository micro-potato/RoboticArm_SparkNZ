using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace DataShow
{
    public partial class ArmSensorTestForm : Form
    {
        private SerialPort _serialPort;
        delegate void DeleString(string arg);
        delegate void DeleVoid();
        double[] Angle = new double[4];

        public struct EulerAngles
        {
            public double roll, pitch, yaw;
        }
        private EulerAngles _latestEulerAngles = new EulerAngles();
        public EulerAngles LatestEulerAngles
        {
            get { return _latestEulerAngles; }
        }

        public ArmSensorTestForm()
        {
            InitializeComponent();
        }

        private void ArmSensorTestForm_Load(object sender, EventArgs e)
        {
            _serialPort = new SerialPort("COM7");
            _serialPort.BaudRate = 115200;
            _serialPort.DataReceived += _serialPort_DataReceived;
            if(!_serialPort.IsOpen)
            {
                _serialPort.Open();
            }
        }

        byte[] RxBuffer = new byte[1000];
        UInt16 usRxLength = 0;
        private void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] byteTemp = new byte[1000];
            try
            {
                UInt16 usLength = 0;
                try
                {
                    usLength = (UInt16)_serialPort.Read(RxBuffer, usRxLength, 700);
                }
                catch (Exception err)
                {
                    throw err;
                }
                usRxLength += usLength;
                while (usRxLength >= 11)
                {
                    RxBuffer.CopyTo(byteTemp, 0);
                    if (!((byteTemp[0] == 0x55) & ((byteTemp[1] & 0x50) == 0x50)))
                    {
                        for (int i = 1; i < usRxLength; i++) RxBuffer[i - 1] = RxBuffer[i];
                        usRxLength--;
                        continue;
                    }
                    if (((byteTemp[0] + byteTemp[1] + byteTemp[2] + byteTemp[3] + byteTemp[4] + byteTemp[5] + byteTemp[6] + byteTemp[7] + byteTemp[8] + byteTemp[9]) & 0xff) == byteTemp[10])
                        DecodeData(byteTemp);
                    for (int i = 11; i < usRxLength; i++) RxBuffer[i - 11] = RxBuffer[i];
                    usRxLength -= 11;
                }

                Thread.Sleep(10);
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        private void DecodeData(byte[] byteData)
        {
            double[] Data = new double[4];

            Data[0] = BitConverter.ToInt16(byteData, 2);
            Data[1] = BitConverter.ToInt16(byteData, 4);
            Data[2] = BitConverter.ToInt16(byteData, 6);
            switch (byteData[1])
            {
                case 0x53:
                    Data[0] = Data[0] / 32768.0 * 180;
                    Data[1] = Data[1] / 32768.0 * 180;
                    Data[2] = Data[2] / 32768.0 * 180;
                    Angle[0] = Math.Round(Data[0],2);//x，pitch，俯仰角
                    Angle[1] = Math.Round(Data[1],2);//y,yaw,偏转角
                    Angle[2] = Math.Round(Data[2],2);//z,roll,翻滚角
                    //Angle[3] = Data[3];
                    _latestEulerAngles.pitch = Angle[0];
                    _latestEulerAngles.yaw = Angle[1];
                    _latestEulerAngles.roll = Angle[2];
                    this.BeginInvoke(new DeleVoid(DisplayEurl));
                    break;
                default:
                    break;
            }
        }

        private void DisplayEurl()
        {
            richTextBox1.Clear();
            this.richTextBox1.AppendText("roll:" + LatestEulerAngles.roll);
            this.richTextBox1.AppendText("pitch:" + LatestEulerAngles.pitch);
            this.richTextBox1.AppendText("yaw:" + LatestEulerAngles.yaw);
            this.richTextBox1.AppendText("\r\n");
            richTextBox1.ScrollToCaret();
            if(richTextBox1.Lines.Length>10000)
            {
                richTextBox1.Clear();
            }
        }

        private void AppendText(string text)
        {
            this.richTextBox1.AppendText(text + "\n");
        }
    }
}
