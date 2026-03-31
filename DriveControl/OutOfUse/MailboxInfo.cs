using System;
using System.Collections.Generic;
using System.Text;

namespace SwarmIntelligence.Logic.DriveControl.OutOfUse
{
    public record class MailboxInfo
    {
        public byte AgentId { get; init; }
        public int Capacity { get; init; }
        public IReadOnlyCollection<DriveCommand>? PendingCommands { get; init; }
        public DriveCommand? CurrentCommand { get; init; }
        public bool IsRunning { get; init; }
    }
}
