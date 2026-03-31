using System;

namespace SwarmIntelligence.Logic.MapControl.EventArgs
{
    public enum WorkerState
    {
        Started,
        Stopped,
    }

    public sealed class WorkerStateChangedEventArgs : System.EventArgs
    {
        public WorkerStateChangedEventArgs(WorkerState state)
        {
            State = state;
        }

        public WorkerState State { get; }
        public DateTime Timestamp { get; } = DateTime.UtcNow;
    }
}
