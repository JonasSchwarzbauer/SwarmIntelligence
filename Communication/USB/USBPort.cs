using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;

namespace SwarmIntelligence.Logic.Communication.USB
{
    public class USBPort
    {
        private static readonly Lazy<USBPort> _instance = new Lazy<USBPort>(() => new USBPort());
        public static USBPort Instance => _instance.Value;
        private USBPort()
        {
            Port = new SerialPort("/dev/ttyUSB0", 115200); // Just the portname is different on the raspi /dev/ttyUSB0
            Port.Open();
        }
        public void ClosePort()
        {
            Port.Close();
        }
        public SerialPort Port { get; private set; }
    }
}
