using System;

namespace SwarmIntelligence.Logic.Communication.DTOs.DriveControl
{
    /// <summary>
    /// DTO for manager state change events.
    /// </summary>
    public record class ManagerStateDto
    {
        public required bool IsRunning { get; init; }
        public required DateTime Timestamp { get; init; }
    }
}
