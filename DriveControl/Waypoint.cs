using System;
using System.Collections.Generic;
using System.Text;

namespace SwarmIntelligence.Logic.DriveControl
{
    public record struct Waypoint(float X, float Y, float Heading, float MaxSpeed);
}
