using SwarmIntelligence.Logic.Communication.Interfaces;
using SwarmIntelligence.Logic.Communication.USB.Interfaces;
using SwarmIntelligence.Logic.MapControl;
using System;
using System.Collections.Generic;
using System.Text;

namespace SwarmIntelligence.Logic.Communication.USB
{
    public class VehicleStateStructConverter : IStructConverter
    {
        public (IStructConvertable ConvStruct, byte MessageId) ConvertStruct(IStructConvertable structIn)
        {
            SlaveToLeadVehicleStateUSBPayload state = (SlaveToLeadVehicleStateUSBPayload)structIn;
            AgentGeoData agentGeoData = new AgentGeoData()
            {
                AgentId = state.origin_vehicle_id,
                Flags = (VehicleFlags)state.flags,
                FrontalDistance = state.frontal_distance,
                Position = new System.Numerics.Vector2(state.x, state.y),
                DwmSuccessRate = state.success_rate_DWM,
                Target = new DriveControl.Waypoint(state.current_target_waypoint[0], state.current_target_waypoint[1], state.current_target_waypoint[2], state.current_target_waypoint[3]),
                DataReceived = DateTime.Now,
                Velocity = state.speed,
                Orientation = state.heading
            };
            return (agentGeoData, 0);
        }
    }
}
