using System;

namespace SwarmIntelligence.Logic.Communication.DTOs.DriveControl
{
    /// <summary>
    /// DTO for error events in the drive control system.
    /// </summary>
    public record class DriveControlErrorDto
    {
        public required byte AgentId { get; init; }
        public required string Message { get; init; }
        public required string Source { get; init; }
        public required string SourceContext { get; init; }
        public required string ExceptionMessage { get; init; }
        public required Exception Exception { get; init; }
        public required DateTime Timestamp { get; init; }
    }
}
