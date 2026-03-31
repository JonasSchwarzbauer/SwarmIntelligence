using System;
using System.Collections.Generic;
using System.Text;

namespace SwarmIntelligence.Logic.Communication.DTOs
{
    /// <summary>
    /// DTO for a waypoint coordinate.
    /// </summary>
    public readonly record struct WaypointDto(
        double X,
        double Y,
        double Heading,
        double MaxSpeed
    );
}
