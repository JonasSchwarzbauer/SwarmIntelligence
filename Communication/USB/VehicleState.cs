using SwarmIntelligence.Logic.Communication.USB.Interfaces;
using SwarmIntelligence.Logic.DriveControl;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SwarmIntelligence.Logic.Communication.USB
{
    [Flags]
    public enum VehicleFlags
    {
        WaypointActive = 1,
        StoppedSensorTimeout = 2,
        StoppedMinDist = 4
    }
    public record struct VehicleState : IUSBReceivable
    {
        public byte OriginVehicleId { get; init; }
        public Waypoint CurrentTargetWaypoint { get; init; }
        public VehicleFlags Flags { get; init; }
        public float SuccessRateDwm { get; init; }
        public float FrontalDistance { get; init; }
        public (float CurrentX, float CurrentY, float CurrentHeading, float CurrentSpeed) CurrentState { get; init; }
        public DateTime ReceivedTimestamp { get; init; }
    }
}