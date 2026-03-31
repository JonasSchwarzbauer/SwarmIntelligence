using SwarmIntelligence.Logic.Communication.Interfaces;
using SwarmIntelligence.Logic.Communication.USB.Interfaces;
using SwarmIntelligence.Logic.DriveControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwarmIntelligence.Logic.Communication
{
    public class DriveCommandStructConverter : IStructConverter
    {
        public (IStructConvertable ConvStruct, byte MessageId) ConvertStruct(IStructConvertable structIn)
        {
            DriveCommand driveCmd = (DriveCommand)structIn;
            LeadToSlaveCommunicationUSBPayload usbStruct = new();
            usbStruct.target_vehicle_id = (byte)driveCmd.AgentId;
            usbStruct.flags = (byte)driveCmd.DriveFlags;
            usbStruct.num_waypoints = (byte)driveCmd.Waypoints.Count;
            usbStruct.path = new float[CommunicationConfig.MAX_WAYPOINTS * CommunicationConfig.WAYPOINT_SIZE];
            for (int i = 0; i < driveCmd.Waypoints.Count; i++)
            {
                usbStruct.path[i * CommunicationConfig.WAYPOINT_SIZE] = driveCmd.Waypoints[i].X;
                usbStruct.path[i * CommunicationConfig.WAYPOINT_SIZE + 1] = driveCmd.Waypoints[i].Y;
                usbStruct.path[i * CommunicationConfig.WAYPOINT_SIZE + 2] = driveCmd.Waypoints[i].Heading;
                usbStruct.path[i * CommunicationConfig.WAYPOINT_SIZE + 3] = driveCmd.Waypoints[i].MaxSpeed;
            }
            return (usbStruct, (byte)MessageStructIDsUSB.LeadSlaveCommunication);
        }
    }
}
