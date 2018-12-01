using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArmController
{
    internal class HanoiMonitor
    {
        internal delegate void DelInt(int arg);
        internal event DelInt PressStateChanged;

        private SerialPortHelper _serialPortHelper;

        internal HanoiMonitor(string comPort)
        {
            _serialPortHelper = new SerialPortHelper(comPort,9600);
            _serialPortHelper.DataIn += DataIn;
        }

        private void DataIn(string data)
        {
            int pressState = -1;
            //down:I(001,1),up:I(001,0)
            if (data.IndexOf("1)") >= 0)
            {
                pressState = 1;
            }
            else if (data.IndexOf("0)") >= 0)
            {
                pressState = 0;
            }

            if(pressState!=-1)
            {
                PressStateChanged?.Invoke(pressState);
            }
            else//invalid data
            {
                throw new Exception("Invalid button data");
            }
        }

        /// <summary>
        /// 1:O(00,01,1)E吸起，0:O(00,01,0)E松开
        /// </summary>
        /// <param name="state">汉诺塔状态</param>
        public void SetHanioState(int state)
        {
            string cmd = string.Format("O(00,01,{0})E", state);
            _serialPortHelper.SendString(cmd);
        }
            
    }
}
