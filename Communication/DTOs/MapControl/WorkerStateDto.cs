using System;

namespace SwarmIntelligence.Logic.Communication.DTOs.MapControl
{
    /// <summary>
    /// DTO for worker state change events.
    /// </summary>
    public record class WorkerStateDto
    {
        public required string State { get; init; }
        public required DateTime Timestamp { get; init; }
    }
}
