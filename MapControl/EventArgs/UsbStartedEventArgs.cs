using System;

namespace SwarmIntelligence.Logic.MapControl.EventArgs
{
    /// <summary>
    /// Event args for notifying when USB data provision is started.
    /// </summary>
    public class UsbStartedEventArgs : System.EventArgs
    {
        public UsbStartedEventArgs(bool success, string portName, int baudRate, string message)
        {
            Success = success;
            PortName = portName;
            BaudRate = baudRate;
            Message = message;
        }

        public bool Success { get; }
        public string PortName { get; }
        public int BaudRate { get; }
        public string Message { get; }
        public DateTime Timestamp { get; } = DateTime.UtcNow;
    }
}
