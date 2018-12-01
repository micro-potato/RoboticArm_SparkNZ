using Helpers;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;

namespace ArmController
{
    public class ResetMonitor
    {
        private SerialPort _serialPort;
        public delegate void Datain(string data);
        public event Datain DataIn;

        public ResetMonitor(string comport)
        {
            _serialPort = new SerialPort(comport);
            _serialPort.BaudRate = 9600;
            _serialPort.DataReceived += DataReceived;
            try
            {
                if (!_serialPort.IsOpen)
                {
                    _serialPort.Open();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        string recordData = "";
        private void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                SerialPort sp = (SerialPort)sender;
                string recData = sp.ReadExisting().Replace(" ", "");
                LogHelper.GetInstance().ShowMsg("ResetButton:" + recData);
                string spliceData = recordData + recData;
                string dealData = string.Empty;
                if (spliceData.IndexOf(')') < 0)
                {
                    recordData = spliceData;
                }
                else
                {
                    dealData = spliceData.Substring(0, spliceData.LastIndexOf(')') + 1);
                    recordData = spliceData.Substring(spliceData.LastIndexOf(')') + 1, spliceData.Length - spliceData.LastIndexOf(')') - 1);


                }
                if (!String.IsNullOrEmpty(dealData) && dealData != "")
                {
                    if (DataIn != null)
                        DataIn(dealData);
                }
                //Thread.Sleep(500);
            }
            catch
            {
                recordData = "";
            }
        }
    }
}
