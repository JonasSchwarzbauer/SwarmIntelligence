using System;

namespace SwarmIntelligence.Logic.Communication.DTOs.MapControl
{
    /// <summary>
    /// DTO for map control error events.
    /// </summary>
    public record class MapControlErrorDto
    {
        public required string Message { get; init; }
        public required string Source { get; init; }
        public required string SourceContext { get; init; }
        public required string ExceptionMessage { get; init; }
        public required Exception Exception { get; init; }
        public required DateTime Timestamp { get; init; }
    }
}
