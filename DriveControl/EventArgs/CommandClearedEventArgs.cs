namespace SwarmIntelligence.Logic.DriveControl.EventArgs
{
    public class CommandClearedEventArgs : System.EventArgs
    {
        public CommandClearedEventArgs(byte agentId)
        {
            AgentId = agentId;
        }

        public byte AgentId { get; }
        public DateTime Timestamp { get; } = DateTime.UtcNow;
    }
}