using SwarmIntelligence.Logic.Navigation;
using System;
using System.Collections.Generic;
using System.Text;

namespace SwarmIntelligence.Logic.Communication.DTOs.UserInputs
{
    public record class FormationPathDto
    {
        public required WaypointGrid[] Waypoints { get; init; }
        public required bool ModeChanged { get; set; }

        public override string ToString()
        {
            var waypointsText = Waypoints is { Length: > 0 }
                ? string.Join(", ", Waypoints)
                : "";

            return $"FormationPathDto {{ Waypoints = [{waypointsText}], ModeChanged = {ModeChanged} }}";
        }
    }
}
