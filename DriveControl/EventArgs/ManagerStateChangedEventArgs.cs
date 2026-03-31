using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwarmIntelligence.Logic.DriveControl.EventArgs
{
    public class ManagerStateChangedEventArgs : System.EventArgs
    {
        public ManagerStateChangedEventArgs(bool isRunning)
        {
            IsRunning = isRunning;
        }

        public bool IsRunning { get; }
        public DateTime Timestamp { get; } = DateTime.UtcNow;
    }
}
