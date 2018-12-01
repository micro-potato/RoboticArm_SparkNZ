using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClientDLL;
using System.Threading;
using Helpers;

namespace ArmController
{
    public class RobotHandler
    {
        private AsyncClient asyncClient;
        private System.Timers.Timer asyncTimer = new System.Timers.Timer();
        private char[] m_endChar = new char[] { '\n'};
        string _ip;
        int _port;
        double _A1k, _A2k, _A3k, _A4k, _A2DownMax, _Y2excludeY1k, _P2excludeP1k;
        public double _A1offset, _A2offset, _A4offset;
        private double[] _prevOffset = new double[6];

        public RobotHandler(string ip, int port)
        {
            _ip = ip;
            _port = port;
            InitAngleKs();
            InitSocket(ip,port);
        }

        public double VerticalValue
        {
            get { return _A2offset; }
        }

        private void InitAngleKs()
        {
            var config = ConfigHelper.GetInstance();
            _A1k = config.A1k;
            _A2k = config.A2k;
            _A3k = config.A3k;
            _A4k = config.A4k;
            _A2DownMax = config.A2DownMax;
            _Y2excludeY1k = config.Y2excludeY1k;
            _P2excludeP1k = config.P2excludeP1k;
        }

        #region Socket
        private void InitSocket(string ip,int port)
        {
            try
            {
                InitAsyncTimer();
                if (this.asyncClient != null)
                {
                    this.asyncClient.Dispose();
                    this.asyncClient.onConnected -= new AsyncClient.Connected(client_onConnected);
                    this.asyncClient.onDisConnect -= new AsyncClient.DisConnect(client_onDisConnect);
                    this.asyncClient.onDataByteIn -= new AsyncClient.DataByteIn(client_onDataByteIn);
                }
                asyncClient = new AsyncClient();
                asyncClient.onConnected += new AsyncClient.Connected(client_onConnected);
                asyncClient.Connect(ip, port);
                asyncClient.onDataByteIn += new AsyncClient.DataByteIn(client_onDataByteIn);
                asyncClient.onDisConnect += new AsyncClient.DisConnect(client_onDisConnect);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        void client_onDataByteIn(byte[] SocketData)
        {
            string cmd = System.Text.Encoding.UTF8.GetString(SocketData);
            //this.Invoke(new DelSetText(SetText), new object[] { cmd });
            string[] dataList = cmd.Split(m_endChar, StringSplitOptions.RemoveEmptyEntries);
            foreach (string data in dataList)
            {
                //this.Invoke(new DelDataDeal(DataDeal), new object[] { data });
            }
        }

        void client_onConnected()
        {
            try
            {
                Thread.Sleep(100);
                asyncTimer.Stop();
                //string message = string.Format("{0}连线!", ip);
                //this.Invoke(new DelSetText(SetText), new object[] { message });
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                throw new Exception(ex.Message);
            }
        }

        void client_onDisConnect()
        {
            string message = string.Format("{0}断线!",_ip);
            //this.Invoke(new DelSetText(SetText), new object[] { message });
            asyncTimer.Start();
        }

        private void asyncTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            ReConnect();
        }

        private void ReConnect()
        {
            asyncClient.Dispose();
            asyncClient.Connect(_ip, _port);
        }

        private void InitAsyncTimer()
        {
            asyncTimer = new System.Timers.Timer();
            asyncTimer.Interval = 1500;
            asyncTimer.Elapsed += new System.Timers.ElapsedEventHandler(asyncTimer_Elapsed);
        }
        #endregion       

        #region Move

        /// <summary>
        /// 移动机械臂，参数为本次的姿态偏移角度
        /// </summary>
        public void MoveArm(double pitch1, double yaw1, double roll1, double pitch2, double yaw2, double roll2)
        {
            double offsetA1 = roll1 * _A1k;//big arm H value,偏转角
            double offsetA2 = pitch1 * _A2k;

            if(Math.Abs(offsetA1/offsetA2)>1.5)//主要移动方向为水平方向
            {
                offsetA2 *= 0.25;
            }
            else if(Math.Abs(offsetA2 / offsetA1) > 1.5)//主要移动方向为垂直方向
            {
                offsetA1 *= 0.25;
            }
            _A1offset += offsetA1;
            _A2offset += offsetA2;

            //小臂移动

            double offsetA4 = (roll2 - roll1)*_A4k;
            _A4offset += offsetA4;
            //处理越界,机械臂最大移动范围
            _A1offset = Math.Round(SetBoundary(_A1offset, 170),1);
            _A2offset = Math.Round(SetBoundary(_A2offset, 120),1);
            _A4offset = Math.Round(SetBoundary(_A4offset, 120),1);

            string datatoSend = string.Format("<A1>{0}</A1><A2>{1}</A2><A3>{2}</A3><A4>{3}</A4><A5>{4}</A5><A6>{5}</A6><A7>{6}</A7>|", _A1offset.ToString(), _A2offset>_A2DownMax?_A2DownMax.ToString():_A2offset.ToString(), "0", _A4offset, "0", "0", "0");//A2offset:向下有保护

            asyncClient.Send(datatoSend);
        }

        private double SetBoundary(double currentValue, int maxValue)
        {
            if (Math.Abs(currentValue) > maxValue)
            {
                currentValue = maxValue * (currentValue / Math.Abs(currentValue));
            }
            return currentValue;
        }

        private bool ArmMoved(double[] offsetData)
        {
            var bigArmOffsetH = Math.Abs(offsetData[2] - _prevOffset[2]);
            var smallArmOffset = Math.Abs(offsetData[5] - _prevOffset[5]);
            var bigArmOffsetV = Math.Abs(offsetData[1] - _prevOffset[1]);

            if (bigArmOffsetH < 3 && smallArmOffset < 3 && bigArmOffsetV < 3)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 计算发送给机械臂的pitch角度
        /// </summary>
        /// <param name="pitch">来自手套的picth</param>
        /// <param name="k">修正系数</param>
        /// <returns></returns>
        private double CalcPitch(double pitch, double k)
        {
            return pitch * k;
        }

        /// <summary>
        /// 计算发送给机械臂的yaw角度
        /// </summary>
        /// <param name="yaw">来自手套的yaw角度</param>
        /// <param name="k">修正系数</param>
        /// <returns></returns>
        private double CalcYaw(double yaw,double k)
        {
            var offsetY = yaw;
            if (Math.Abs(offsetY) >= 180)
            {
                if (offsetY > 0)
                {
                    offsetY = offsetY - 360 + 1;
                }
                else
                {
                    offsetY = 360 + offsetY - 1;
                }
            }
            return offsetY*k;
        }

        public void ResetOffset()
        {
            _A1offset = 0;
            _A2offset = 0;
            _A4offset = 0;
        }

        /// <summary>
        /// A4轴旋转度数
        /// </summary>
        /// <param name="offsetP2excludP1">小臂水平偏移</param>
        /// <param name="offsetY2excludeY1">小臂竖直偏移</param>
        /// <param name="_A4k"></param>
        /// <returns></returns>
        private double CalcA4(double offsetY2excludeY1, double _A4k)
        {
            double offsetLength = Math.Abs(offsetY2excludeY1);//移动距离,只考虑小臂水平
            int dir = offsetY2excludeY1 >= 0 ? 1 : -1;
            return offsetLength * dir * _A4k;
        }

        public void MoveArm(string data)
        {
            try
            {
                asyncClient.Send(data + "|");
                LogHelper.GetInstance().ShowMsg("send to IIWA:=============" + data + "\n");
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        #endregion

        public void NotifyPower(int powerType)
        {
            asyncClient.Send(string.Format("<Power>{0}</Power>|", powerType.ToString()));
        }

        /// <summary>
        /// 机械臂下探控制
        /// </summary>
        /// <param name="reachType">//1 下探，0 恢复</param>
        public void ReachtoObject(int reachType)
        {
            string msg = string.Format("<Reach>{0}</Reach>|", reachType);
            asyncClient.Send(msg);
            LogHelper.GetInstance().ShowMsg("send to IIWA:" + msg);
        }
    }
}
