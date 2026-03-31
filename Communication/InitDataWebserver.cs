using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace SwarmIntelligence.Logic.Communication
{
    // The data sent from the webserver
    public record struct InitDataWebserver
    {
        public float CompassOffset { get; init; }

        // 3 Anchor-Positions (first index) and 2 floats per Position
        // Format: [(0,0), (0,yMax), (xMax,0)] => Order is Anchor 1, 2, 3
        public AnchorPosition[] AnchorPositions { get; init; }
        public byte RequiredSlavesAmount { get; init; }
    }

    public record struct AnchorPosition(float X, float Y);
}
