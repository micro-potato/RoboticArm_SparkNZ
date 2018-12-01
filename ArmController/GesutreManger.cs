using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArmController
{
    public class GesutreManger
    {
        private SensorMonitor _upperArmMonitor;
        private SensorMonitor _foreArmMonitor;
        private HanoiMonitor _hanioMonitor;

        private string _upperArmComport = "";
        private string _foreArmComport = "";
        private string _buttonComport = "";

        private int _adjustElapse = 0;

        private EulerAngleSpeed _latestUpperArmAngleSpeed;
        private EulerAngleSpeed _latestForeArmAngleSpeed;
 
        private double _speedK = 0.07;
        private double _minSpeed = 5;
        private double _maxSpeed = 150;

        public delegate void DeleInt(int arg);
        public event DeleInt ButtonStateChange;
        public delegate void DelVoid();
        public event DelVoid PressReset;

        private System.Timers.Timer _adjustTimer;
        private System.Timers.Timer _monitorTimer;
        private int _monitorInterval = 40;

        public delegate void DeleOffsetAngle(EulerAngle upperArm, EulerAngle foreArm);
        public event DeleOffsetAngle GestureUpdated;

        public GesutreManger(string upperArmComport,string foreArmComport,string buttonArmComport,double speedK=0.07,double minSpeed=5,int monitorInterval = 40, int adjustTime = 3)
        {
            _upperArmComport = upperArmComport;
            _foreArmComport = foreArmComport;
            _buttonComport = buttonArmComport;

            _adjustTimer = new System.Timers.Timer(2000);
            _adjustTimer.Elapsed += AdjustFinish;
            try
            {
                _hanioMonitor = new HanoiMonitor(_buttonComport);
                _upperArmMonitor = new SensorMonitor(_upperArmComport);
                _foreArmMonitor = new SensorMonitor(_foreArmComport);
                
                _upperArmMonitor.GetureResultUpdate += OnUpperArmUpdate;
                _foreArmMonitor.GetureResultUpdate += OnForeArmUpdate;
                _hanioMonitor.PressStateChanged += ButtonPressChanged;

                _monitorInterval = monitorInterval;
                _monitorTimer = new System.Timers.Timer(_monitorInterval);
                _monitorTimer.Elapsed += OnUpdateGestureResultTime;

                _adjustElapse = adjustTime;

                _speedK = speedK;
                _minSpeed = minSpeed;
            }
            catch(Exception e)
            {
                throw new Exception("Wearable Devices Init Error:" + e.Message);
            }
        }

        private void OnPressReset()
        {
            PressReset?.Invoke();
        }

        /// <summary>
        /// button state
        /// </summary>
        /// <param name="arg">0:release,1:press</param>
        private void ButtonPressChanged(int arg)
        {
            ButtonStateChange?.Invoke(arg);
        }

        private void OnForeArmUpdate(EulerAngleSpeed angleSpeed)
        {
            //_latestForeArmAngleSpeeds = angleSpeed;
            angleSpeed = RangeSpeed(angleSpeed, _maxSpeed);
            _latestForeArmAngleSpeed = AverageAnglesSpeed(_latestForeArmAngleSpeed, angleSpeed);
        }

        private void OnUpperArmUpdate(EulerAngleSpeed angleSpeed)
        {
            //_latestUpperArmAngleSpeeds = angles;
            angleSpeed = RangeSpeed(angleSpeed, _maxSpeed);
            _latestUpperArmAngleSpeed = AverageAnglesSpeed(_latestUpperArmAngleSpeed, angleSpeed);
        }

        private EulerAngleSpeed RangeSpeed(EulerAngleSpeed angleSpeed, double maxSpeed)
        {
            if(Math.Abs(angleSpeed.PitchSpeed)>maxSpeed)
            {
                angleSpeed.PitchSpeed = angleSpeed.PitchSpeed / Math.Abs(angleSpeed.PitchSpeed) * maxSpeed;
            }
            if (Math.Abs(angleSpeed.YawSpeed) > maxSpeed)
            {
                angleSpeed.YawSpeed = angleSpeed.YawSpeed / Math.Abs(angleSpeed.YawSpeed) * maxSpeed;
            }
            if (Math.Abs(angleSpeed.RollSpeed) > maxSpeed)
            {
                angleSpeed.RollSpeed = angleSpeed.RollSpeed / Math.Abs(angleSpeed.RollSpeed) * maxSpeed;
            }
            return angleSpeed;
        }

        private EulerAngleSpeed AverageAnglesSpeed(EulerAngleSpeed speed1, EulerAngleSpeed speed2)
        {
            return new EulerAngleSpeed { PitchSpeed = (speed1.PitchSpeed + speed2.PitchSpeed) / 2,YawSpeed= (speed1.YawSpeed + speed2.YawSpeed) / 2,RollSpeed= (speed1.RollSpeed + speed2.RollSpeed) / 2 };
        }

        private void StartAdjust()
        {
            _adjustTimer.Start();
        }

        public void StartMonitor()
        {
            StartAdjust();
        }

        public void StopMonitor()
        {
            _monitorTimer.Stop();
        }

        private void AdjustFinish(object sender, System.Timers.ElapsedEventArgs e)
        {
            _adjustTimer.Stop();
            _monitorTimer.Start();
        }

        /// <summary>
        /// 获取最新速度数据并发送
        /// </summary>
        private void OnUpdateGestureResultTime(object sender, System.Timers.ElapsedEventArgs e)
        {
            EulerAngle upperOffset = EulerAngleOffset(_latestUpperArmAngleSpeed);
            EulerAngle foreOffset = EulerAngleOffset(_latestForeArmAngleSpeed);
            GestureUpdated?.Invoke(upperOffset, foreOffset);
        }

        /// <summary>
        /// 叠加两个欧拉角
        /// </summary>
        /// <param name="angle1"></param>
        /// <param name="angle2"></param>
        /// <returns></returns>
        public EulerAngle AddOffset(EulerAngle angle1, EulerAngle angle2)
        {
            return new EulerAngle { Pitch = angle1.Pitch + angle2.Pitch, Yaw = angle1.Yaw + angle2.Yaw, Roll = angle1.Roll + angle2.Roll };
        }

        /// <summary>
        /// 根据速度计算角度偏移量
        /// </summary>
        /// <param name="speed">传感器速度监测值</param>
        /// <returns></returns>
        public EulerAngle EulerAngleOffset(EulerAngleSpeed speed)
        {
            EulerAngle angle = new EulerAngle();
            if (IsMove(speed))
            {
                angle.Pitch = speed.PitchSpeed * _speedK;
                angle.Yaw = speed.YawSpeed * _speedK;
                angle.Roll = speed.RollSpeed * _speedK;
            }
            return angle;
        }

        private bool IsMove(EulerAngleSpeed speed)
        {
            if (Math.Abs(speed.PitchSpeed) < _minSpeed && Math.Abs(speed.YawSpeed) < _minSpeed && Math.Abs(speed.RollSpeed) < _minSpeed) return false;
            else return true;
        }


        /// <summary>
        /// 1吸起，0松开
        /// </summary>
        /// <param name="state">汉诺塔状态</param>
        public void SetHanioState(int state)
        {
            _hanioMonitor.SetHanioState(state);
        }

        public void Dispose()
        {
            _upperArmMonitor.Dispose();
            _foreArmMonitor.Dispose();
        }
    }

    public struct EulerAngleSpeed
    {
        public double PitchSpeed, YawSpeed, RollSpeed;
    }

    public struct EulerAngle
    {
        public double Pitch, Yaw, Roll;
    }
}
