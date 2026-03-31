using System;
using System.Collections.Generic;
using System.Text;

namespace SwarmIntelligence.Logic.MapControl.EventArgs
{
    public class BufferInformationEventArgs : System.EventArgs
    {
        public BufferInformationEventArgs(bool success, int bufferCount, int bufferCapacity)
        {
            Success = success;
            BufferCount = bufferCount;
            BufferCapacity = bufferCapacity;
        }
        public bool Success { get; }
        public int BufferCount { get; }
        public int BufferCapacity { get; }
        public float BufferUsagePercentage => BufferCapacity == 0 ? 0 : (float)BufferCount / BufferCapacity * 100;
        public DateTime Timestamp { get; } = DateTime.UtcNow;
    }
}
