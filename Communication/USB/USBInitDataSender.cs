using SwarmIntelligence.Logic.Communication.Factories;
using SwarmIntelligence.Logic.Communication.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwarmIntelligence.Logic.Communication
{
    public class USBInitDataSender : USBSender<InitData>
    {
        public USBInitDataSender() : base(StructConverterFactory.CreateStructConverter(StructConverterTypes.InitDataConverter))
        {
        }
    }
}
