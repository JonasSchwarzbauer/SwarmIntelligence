using SwarmIntelligence.Logic.Communication.Interfaces;
using SwarmIntelligence.Logic.Communication.USB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace SwarmIntelligence.Logic.Communication.Factories
{
    public static class StructConverterFactory
    {
        public static IStructConverter CreateStructConverter(StructConverterTypes type)
        {
            return type switch
            {
                StructConverterTypes.DriveCommandConverter => new DriveCommandStructConverter(),
                StructConverterTypes.InitDataConverter => new InitDataStructConverter(),
                StructConverterTypes.VehicleStateConverter => new VehicleStateStructConverter(),
                _ => throw new NotSupportedException("Invalid Enum Type for StructConverterFactory")
            };
        }
    }
}
