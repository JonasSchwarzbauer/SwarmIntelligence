using SwarmIntelligence.Logic.Navigation;
using System;
using System.Collections.Generic;
using System.Text;

namespace SwarmIntelligence.Logic.Communication.DTOs.UserInputs
{
    public record class VehicleTargetsDto
    {
        public required WaypointGrid[] Waypoints { get; init; }
        public required byte AgentId { get; init; }
        public required bool OverRide { get; init; }
        public required bool ModeChanged { get; set; }

        public override string ToString()
        {
            var waypointsText = Waypoints is { Length: > 0 }
                ? string.Join(", ", Waypoints)
                : "";

            return $"VehicleTargetsDto {{ Waypoints = [{waypointsText}], AgentId = {AgentId}, OverRide = {OverRide}, ModeChanged = {ModeChanged} }}";
        }
    }
}
