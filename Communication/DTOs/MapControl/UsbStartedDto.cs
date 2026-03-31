using System;

namespace SwarmIntelligence.Logic.Communication.DTOs.MapControl
{
    /// <summary>
    /// DTO for USB started events.
    /// </summary>
    public record class UsbStartedDto
    {
        public required bool Success { get; init; }
        public required string PortName { get; init; }
        public required int BaudRate { get; init; }
        public required string Message { get; init; }
        public required DateTime Timestamp { get; init; }
    }
}
