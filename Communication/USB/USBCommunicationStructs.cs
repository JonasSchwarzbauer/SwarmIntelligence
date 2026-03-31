using SwarmIntelligence.Logic.Communication.Interfaces;
using SwarmIntelligence.Logic.Communication.USB.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SwarmIntelligence.Logic.Communication
{
    // All of the following structs use public fields, because the Marshal class would not work correctly 
    // for properties => corrupted / incomplete byte stream would be the result
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PacketHeaderUSB
    {
        // Must always be 0xAA
        public byte start;
        public byte message_struct_id;
        public byte payload_length;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LeadToSlaveCommunicationUSBPayload : IUSBSendStruct
    {
        public byte target_vehicle_id;
        public byte num_waypoints;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = CommunicationConfig.MAX_WAYPOINTS * CommunicationConfig.WAYPOINT_SIZE)]
        public float[] path; // Format of one WP: {x,y,heading,speed}
        public byte flags;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LeadToSlaveInitDataUSBPayload : IUSBSendStruct
    {
        public float compass_offset;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = CommunicationConfig.COORDINATE_SIZE * CommunicationConfig.NUMBER_OF_ANCHORS)]
        public float[] anchor_positions;
        public byte defaultTimesToMeasureDwm;
        public byte requiredSlavesAmount;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct SlaveToLeadVehicleStateUSBPayload :  IUSBReceiveStruct
    {
        public byte origin_vehicle_id;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = CommunicationConfig.WAYPOINT_SIZE)]
        public float[] current_target_waypoint; // [x, y, heading, maxSpeed]
        public byte flags;
        public float x;
        public float y;
        public float success_rate_DWM;
        public float speed;
        public float heading;
        public float frontal_distance;
    };
    public enum MessageStructIDsUSB
    {
        LeadSlaveInitData = 1,
        SlaveLeadVehicleState = 2,
        LeadSlaveCommunication = 3
    };
}
