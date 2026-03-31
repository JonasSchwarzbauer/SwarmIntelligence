using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwarmIntelligence.Logic.DriveControl.EventArgs
{
    public class AgentRegistrationEventArgs : System.EventArgs
    {
        public AgentRegistrationEventArgs(byte id, RegistrationType type)
        {
            Id = id;
            Type = type;
        }

        public byte Id { get; }
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        public RegistrationType Type { get; }
    }

    public enum RegistrationType
    {
        Registered,
        Unregistered
    }
}
