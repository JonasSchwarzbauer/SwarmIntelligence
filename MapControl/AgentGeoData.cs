using SwarmIntelligence.Logic.Communication.USB;
using SwarmIntelligence.Logic.Communication.USB.Interfaces;
using SwarmIntelligence.Logic.DriveControl;
using System.Numerics;

namespace SwarmIntelligence.Logic.MapControl
{
    public readonly record struct AgentGeoData(
        byte AgentId,
        Vector2 Position,
        float Orientation,
        float Velocity,
        float FrontalDistance,
        Waypoint Target,
        VehicleFlags Flags,
        float DwmSuccessRate,
        DateTime DataReceived
    ) : IStructConvertable;
}