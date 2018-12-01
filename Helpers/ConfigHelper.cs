using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Helpers
{
    public class ConfigHelper
    {
        public string MagnetComPort { get; set; }
        public int JointTimer { get; set; }
        public double A1k { get; set; }
        public double A2k { get; set; }
        public double A3k { get; set; }
        public double A4k { get; set; }
        public double A2DownMax { get; set; }
        public double Y2excludeY1k { get; set; }
        public double P2excludeP1k { get; set; }
        public int ReachedTime { get; set; }
        public int PowerSettedTime { get; set; }
        public int CarryFinishedTime { get; set; }

        //NewZealand ver
        public string ButtonPort { get; set; }
        public string UpperArmPort { get; set; }
        public string ForeArmPort { get; set; }
        //public string MagnetPort { get; set; }
        public string RobotIP { get; set; }
        public int RobotPort { get; set; }
        public double SpeedK { get; set; }
        public double MinSpeed { get; set; }
        //public double VerticalSafetyValue { get; set; }
        public double DrillDownRange { get; set; }
        public string ResetComm { get; set; }

        private static ConfigHelper _configHelper;
        private ConfigHelper()
        {

        }

        public static ConfigHelper GetInstance()
        {
            if (_configHelper == null)
            {
                _configHelper = new ConfigHelper();
            }
            return _configHelper;
        }

        public void ResolveConfig(string configPath)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(configPath);
            this.JointTimer = Convert.ToInt32(xmlDocument.SelectSingleNode("Data/JointTimer").InnerText);
            this.A1k = Convert.ToDouble(xmlDocument.SelectSingleNode("Data/A1k").InnerText);
            this.A2k = Convert.ToDouble(xmlDocument.SelectSingleNode("Data/A2k").InnerText);
            this.A3k = Convert.ToDouble(xmlDocument.SelectSingleNode("Data/A3k").InnerText);
            this.A4k = Convert.ToDouble(xmlDocument.SelectSingleNode("Data/A4k").InnerText);
            this.A2DownMax = Convert.ToDouble(xmlDocument.SelectSingleNode("Data/A2DownMax").InnerText);
            this.Y2excludeY1k = Convert.ToDouble(xmlDocument.SelectSingleNode("Data/Y2excludeY1k").InnerText);
            this.P2excludeP1k = Convert.ToDouble(xmlDocument.SelectSingleNode("Data/P2excludeP1k").InnerText);
            //this.MagnetComPort = xmlDocument.SelectSingleNode("Data/ComPort").InnerText;
            this.ReachedTime = Convert.ToInt32(xmlDocument.SelectSingleNode("Data/ReachedTime").InnerText);
            this.PowerSettedTime = Convert.ToInt32(xmlDocument.SelectSingleNode("Data/PowerSettedTime").InnerText);
            this.CarryFinishedTime = Convert.ToInt32(xmlDocument.SelectSingleNode("Data/CarryFinishedTime").InnerText);

            this.ButtonPort = xmlDocument.SelectSingleNode("Data/ButtonPort").InnerText;
            this.UpperArmPort = xmlDocument.SelectSingleNode("Data/UpperArmPort").InnerText;
            this.ForeArmPort = xmlDocument.SelectSingleNode("Data/ForeArmPort").InnerText;
            //this.MagnetPort = xmlDocument.SelectSingleNode("Data/MagnetPort").InnerText;
            this.RobotIP = xmlDocument.SelectSingleNode("Data/RobotIP").InnerText;
            this.RobotPort = Convert.ToInt32(xmlDocument.SelectSingleNode("Data/RobotPort").InnerText);

            this.SpeedK = Convert.ToDouble(xmlDocument.SelectSingleNode("Data/SpeedK").InnerText);
            this.MinSpeed = Convert.ToDouble(xmlDocument.SelectSingleNode("Data/MinSpeed").InnerText);
            //this.VerticalSafetyValue = Convert.ToDouble(xmlDocument.SelectSingleNode("Data/VerticalSafetyValue").InnerText);
            this.DrillDownRange = Convert.ToDouble(xmlDocument.SelectSingleNode("Data/DrillDownRange").InnerText);
            this.ResetComm= xmlDocument.SelectSingleNode("Data/ResetComm").InnerText;
        }
    }
}
