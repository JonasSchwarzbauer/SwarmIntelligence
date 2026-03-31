using SwarmIntelligence.Logic.Communication.Factories;
using SwarmIntelligence.Logic.Communication.Interfaces;
using SwarmIntelligence.Logic.DriveControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwarmIntelligence.Logic.Communication
{
    public class USBDriveCommandSender : USBSender<DriveCommand>
    {
        public USBDriveCommandSender() : base(StructConverterFactory.CreateStructConverter(StructConverterTypes.DriveCommandConverter))
        {
        }
    }
}
