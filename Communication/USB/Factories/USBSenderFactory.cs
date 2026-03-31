using SwarmIntelligence.Logic.Communication.Interfaces;
using SwarmIntelligence.Logic.Communication.USB.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace SwarmIntelligence.Logic.Communication.USB.Factories
{
    public enum USBSenderTypes
    {
        DriveCommandSender,
        InitDataSender
    }
    public static class USBSenderFactory
    {
        public static IUSBSender CreateUSBSender(USBSenderTypes type)
        {
            return type switch
            {
                USBSenderTypes.DriveCommandSender => new USBDriveCommandSender(),
                USBSenderTypes.InitDataSender => new USBInitDataSender(),
                _ => throw new InvalidDataException("The provided enum is not supported")
            };
        }
    }
}
