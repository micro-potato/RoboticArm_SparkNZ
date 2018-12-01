using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ArmController;
using Helpers;

namespace MechanicalArm
{
    public partial class MainForm : Form, ILog
    {
        private ConfigHelper _configs;
        private bool _isGestureDetected=false;

        private RobotHandler _robotHandler;
        private delegate void deleString(string arg);

        private GesutreManger _gesutreManger;

        //moving hanoi
        private System.Timers.Timer _reachObjectTimer;
        private int _reachTimerTicked = 0;
        private int _currentPressState = 0;
        int _reachedTime = 3;
        int _powerSettedTime = 6;
        int _carryFinishedTime = 8;
        bool _isMovingHanio = false;

        //reset
        ResetMonitor _resetMonitor;

        public MainForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LogHelper.GetInstance().RegLog(this);
            InitConfig();
            InitReachTimer();
            InitRobotController();
            InitHanoiTime();
            try
            {
                InitGestureMotitor();
                _resetMonitor = new ResetMonitor(_configs.ResetComm);
                _resetMonitor.DataIn += ResetMonitorDataIn;
            }
            catch(Exception ex)
            {
                LogHelper.GetInstance().ShowMsg("Unable to capture gesture:"+ex.Message);
            }
        }

        private void ResetMonitorDataIn(string data)
        {
            //down:I(001,1),up:I(001,0)
            LogHelper.GetInstance().ShowMsg(string.Format("ResetButton:{0}", data));
            if (data.IndexOf("0)") >= 0)//press reset,realse ignore
            {
                if(_isGestureDetected)
                {
                    StopDetect();
                }
                else
                {
                    StartDetect();
                }
            }
        }

        private void InitConfig()
        {
            _configs = ConfigHelper.GetInstance();
            _configs.ResolveConfig(System.Windows.Forms.Application.StartupPath + @"\config.xml");
        }

        #region Hanoi
        private void InitHanoiTime()
        {
            _reachedTime = ConfigHelper.GetInstance().ReachedTime;
            _powerSettedTime = ConfigHelper.GetInstance().PowerSettedTime;
            _carryFinishedTime = ConfigHelper.GetInstance().CarryFinishedTime;
        }

        private void InitReachTimer()
        {
            _reachObjectTimer = new System.Timers.Timer(500);
            _reachObjectTimer.Elapsed += new System.Timers.ElapsedEventHandler(ReachTimerTicked);
        }

        /// <summary>
        /// 抓取过程，关键时间点：等待下探时间，通电时间，恢复手套控制时间
        /// </summary>
        void ReachTimerTicked(object sender, System.Timers.ElapsedEventArgs e)
        {
            _reachTimerTicked++;
            if (_reachTimerTicked < _reachedTime)//wait for move
            {
                
            }
            else if (_reachTimerTicked == _reachedTime)//notify power to Robot
            {
                _robotHandler.NotifyPower(_currentPressState);
                LogHelper.GetInstance().ShowMsg("通知机械臂下移，等待下移完成,Power:" + _currentPressState);
            }
            else if (_reachTimerTicked == _powerSettedTime)//set power
            {
                _gesutreManger.SetHanioState(_currentPressState);
                _reachTimerTicked++;
                LogHelper.GetInstance().ShowMsg(string.Format("变更电磁铁状态至{0}，等待抓取/放置完成。。。", _currentPressState));
            }
            else if (_reachTimerTicked == _carryFinishedTime)//finish,give back control right to glove
            {
                _reachObjectTimer.Stop();
                _robotHandler.ReachtoObject(0);
                SetReflecttoArmMove(true);
                _reachTimerTicked = 0;
                _isMovingHanio = false;
                LogHelper.GetInstance().ShowMsg("抓取/放置完成，将机械臂移动控制权交还手套。。。");
            }
        }

        private void BeginReachtoObject()
        {
            _isMovingHanio = true;
            SetReflecttoArmMove(false);
            _robotHandler.ReachtoObject(1);//begin move
            _reachObjectTimer.Start();
            LogHelper.GetInstance().ShowMsg("通知机械臂移动到最近点上方。。。");
        }

        private void OnButtonStateChange(int targetState)
        {
            LogHelper.GetInstance().ShowMsg(string.Format("检测到按钮：{0}", targetState.ToString()));
            if (_isMovingHanio)
                return;
            if (targetState == _currentPressState)
                return;
            _currentPressState = targetState;
            if (_currentPressState == 0)
                LogHelper.GetInstance().ShowMsg("Button Released");
            else if (_currentPressState == 1)
                LogHelper.GetInstance().ShowMsg("Button Pressed");
            BeginReachtoObject();
        }
        #endregion

        #region RobotMove
        private void InitGestureMotitor()
        {
            _gesutreManger = new GesutreManger(_configs.UpperArmPort, _configs.ForeArmPort, _configs.ButtonPort,_configs.SpeedK, _configs.MinSpeed,_configs.JointTimer);
            _gesutreManger.PressReset += OnPressReset;
            _gesutreManger.ButtonStateChange += OnButtonStateChange;
            _gesutreManger.GestureUpdated += OnGestureUpdated;
        }

        private void OnPressReset()
        {
            if(_isGestureDetected)
            {
                StopDetect();
            }
            else
            {
                StartDetect();
            }
        }

        private void StartDetect()
        {
            _isGestureDetected = true;
            _gesutreManger.StartMonitor();
        }

        private void StopDetect()
        {
            _isGestureDetected = false;
            _gesutreManger.StopMonitor();
            string toSend = "<A1>0</A1><A2>0</A2><A3>0</A3><A4>0</A4><A5>0</A5><A6>0</A6><A7>0</A7>";
            _robotHandler.MoveArm(toSend);
            _robotHandler.ResetOffset();
        }

        /// <summary>
        /// 姿态数据更新，roll水平移动，pitch竖直移动
        /// </summary>
        /// <param name="upperArmOffsetThisTime">本次大臂偏移角度</param>
        /// <param name="foreArmOffsetThisTime">本次小臂偏移角度</param>
        private void OnGestureUpdated(EulerAngle upperArmOffsetThisTime, EulerAngle foreArmOffsetThisTime)
        {
            this.Invoke(new MethodInvoker(delegate ()
            {
                this.lbroll1.Text = upperArmOffsetThisTime.Roll.ToString();
                this.lbPitch1.Text = upperArmOffsetThisTime.Pitch.ToString(); this.lbYaw1.Text = upperArmOffsetThisTime.Yaw.ToString(); this.lbRoll2.Text = foreArmOffsetThisTime.Roll.ToString(); this.lbPitch2.Text = foreArmOffsetThisTime.Pitch.ToString(); this.lbYaw2.Text = foreArmOffsetThisTime.Yaw.ToString();//打印根据传感器角速度计算的偏移量
            }));
            _robotHandler.MoveArm(upperArmOffsetThisTime.Pitch, upperArmOffsetThisTime.Yaw, upperArmOffsetThisTime.Roll, foreArmOffsetThisTime.Pitch, foreArmOffsetThisTime.Yaw, foreArmOffsetThisTime.Roll);
        }
        #endregion

        private void InitRobotController()
        {
            _robotHandler=new RobotHandler(ConfigHelper.GetInstance().RobotIP, ConfigHelper.GetInstance().RobotPort);
        }

        private void SetReflecttoArmMove(bool isReflecttoArmMove)
        {
            if (!isReflecttoArmMove)
            {
                try
                {
                    //不响应姿势更新
                    _gesutreManger.GestureUpdated -= OnGestureUpdated;
                }
                catch (Exception e)
                {
                    throw new Exception(e.Message);
                }
            }
            else
            {
                try
                {
                    //响应姿势更新
                    _gesutreManger.GestureUpdated += OnGestureUpdated;
                }
                catch (Exception e)
                {
                    throw new Exception(e.Message);
                }
            }
        }

        /// <summary>
        /// 开始获取手套数据，操作机械臂
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartAdjust_Click(object sender, EventArgs e)
        {
            StartDetect();
            
        }

        private void EndAdjust_Click(object sender, EventArgs e)
        {
            StopDetect();
            
        }

        public void ShowLog(string msg)
        {
            this.Invoke(new deleString(SetText), msg);
        }

        private void SetText(string text)
        {
            this.InfoText.AppendText(text + "\n");
            if (InfoText.Lines.Length > 5000)
            {
                InfoText.Clear();
            }
            this.InfoText.ScrollToCaret();
        }

        #region Test
        private void Move_Click(object sender, EventArgs e)
        {
            string toSend = string.Format("<A1>{0}</A1><A2>{1}</A2><A3>{2}</A3><A4>{3}</A4><A5>{4}</A5><A6>{5}</A6><A7>{6}</A7>", textBox1.Text.Trim(), textBox2.Text.Trim(), textBox3.Text.Trim(), textBox4.Text.Trim(), textBox5.Text.Trim(), textBox6.Text.Trim(), textBox7.Text.Trim());
            _robotHandler.MoveArm(toSend);
        }

        private void PressDown_Click(object sender, EventArgs e)
        {
            
        }

        private void PressUp_Click(object sender, EventArgs e)
        {
            
        }

        private void Reach_Click(object sender, EventArgs e)
        {
            //try
            //{
            //    _robotHandler.ReachtoObject(1);
            //}
            //catch (Exception ex)
            //{
            //    throw ex;
            //}
        }

        private void ClearLog_Click(object sender, EventArgs e)
        {
            this.InfoText.Clear();
        }
        #endregion

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                _gesutreManger.Dispose();
            }
            catch(Exception ex)
            {

            }
            
        }

        private void button3_Click(object sender, EventArgs e)
        {
            _gesutreManger.SetHanioState(1);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            _gesutreManger.SetHanioState(0);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            
        }

        private void button5_Click(object sender, EventArgs e)
        {

        }
    }
}
