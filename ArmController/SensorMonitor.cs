using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;

namespace ArmController
{
    internal class SensorMonitor
    {
        private SerialPort _serialPort;

        private EulerAngleSpeed _sensorResult;

        internal delegate void DeleEulerAngles(EulerAngleSpeed speed);
        internal event DeleEulerAngles GetureResultUpdate;

        private bool _isDetecting = false;

        internal SensorMonitor(string comport)
        {
            _serialPort = new SerialPort(comport);
            _serialPort.BaudRate = 115200;
            _serialPort.DataReceived += _serialPort_DataReceived;
            try
            {
                if (!_serialPort.IsOpen)
                {
                    _serialPort.Open();
                }
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
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
                    if (!_isDetecting)
                    { Thread.Sleep(10);
                        _serialPort.DiscardInBuffer();
                        //_serialPort.DiscardOutBuffer();
                        return;
                    }
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
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private double LastTime = 0;
        private DateTime TimeStart = DateTime.Now;
        private void DecodeData(byte[] byteData)
        {
            
            double TimeElapse = (DateTime.Now - TimeStart).TotalMilliseconds / 1000;
            double[] Data = new double[4];

            Data[0] = BitConverter.ToInt16(byteData, 2);
            Data[1] = BitConverter.ToInt16(byteData, 4);
            Data[2] = BitConverter.ToInt16(byteData, 6);
            switch (byteData[1])
            {
                case 0x52://角速度
                    Data[0] = Data[0] / 32768.0 * 2000;
                    Data[1] = Data[1] / 32768.0 * 2000;
                    Data[2] = Data[2] / 32768.0 * 2000;
                    _sensorResult.PitchSpeed = Math.Round(Data[0], 2);//x，pitch，俯仰角速度
                    _sensorResult.YawSpeed = Math.Round(Data[1], 2);//y,yaw,偏转角速度
                    _sensorResult.RollSpeed = Math.Round(Data[2], 2);//z,roll,翻滚角速度
                    if ((TimeElapse - LastTime) < 0.1) return;//间隔过短，不更新
                    LastTime = TimeElapse;
                    GetureResultUpdate?.Invoke(_sensorResult);
                    break;
                default:
                    break;
            }
        }

        internal void Dispose()
        {
            if(_serialPort.IsOpen)
            {
                _serialPort.Close();
            }
            _serialPort.Dispose();
        }

        internal void StartRecieveData()
        {
            _isDetecting = true;
        }

        internal void StopRecieveData()
        {
            _isDetecting = false;
        }
    }
}
