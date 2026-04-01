using SwarmIntelligence.Logic.Communication;
using System;
using System.Collections.Generic;
using System.Text;

namespace SwarmIntelligence.Logic.Setup
{
    public class SwarmSettings
    {
        // Preset values => TODO: Put into .config file!
        // All values are not tested => Need to be configured!

        public float GridSize { get; init; } = 0.25f; // 0.25f

        public int NavigatorCycleMs { get; init; } = 150;
        public int FormationControllerMs { get; init; } = 250;
        public int ToleratedVehicleInactivenessMs { get; init; } = 10000;

        public float NavigatorMaxFrontalDistanceObstacleDetection { get; init; } = 0.5f;

        public float FormationPathWaypointsDistanceShapeSizeFactor { get; init; } = 1.0f;
        public float FormationBendProximityThresholdForm { get; init; } = 0.2f;
        public float FormationAllowedDeviationRadiusObstacleAvoidanceMeters { get; init; } = 0.5f;
        public float BypassOnRouteTargetTolerance { get; init; } = 0.5f;
        public float NavigatorAcceptReachedTargetRadius { get; init; } = 0.5f;

        public byte DefaultTimesToMeasureDwm { get; init; } = 3;

        public byte NumberOfAgents { get; set; }
        public float XFieldSize { get; set; }
        public float YFieldSize { get; set; }

        public float CompassOffset { get; init; }
        public AnchorPosition[] AnchorPositions { get; init; } = new AnchorPosition[6];
    }
}