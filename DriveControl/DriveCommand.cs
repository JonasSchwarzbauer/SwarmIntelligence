using SwarmIntelligence.Logic.Communication.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwarmIntelligence.Logic.DriveControl
{
    public record struct DriveCommand : IUSBSendable
    {
        public byte AgentId { get; init; }
        public DriveFlags DriveFlags { get; init; }
        public List<Waypoint> Waypoints { get; init; }
        public DateTime TimestampCreated { get; init; } 
    }
}
