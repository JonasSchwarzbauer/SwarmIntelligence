using System;

namespace SwarmIntelligence.Logic.Communication.DTOs.MapControl
{
    /// <summary>
    /// DTO for buffer information events.
    /// </summary>
    public record class BufferInformationDto
    {
        public required bool Success { get; init; }
        public required int BufferCount { get; init; }
        public required int BufferCapacity { get; init; }
        public required float BufferUsagePercentage { get; init; }
        public required DateTime Timestamp { get; init; }
    }
}
