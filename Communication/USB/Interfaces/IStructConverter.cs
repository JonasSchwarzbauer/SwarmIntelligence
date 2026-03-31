using SwarmIntelligence.Logic.Communication.USB.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwarmIntelligence.Logic.Communication.Interfaces
{
    public enum StructConverterTypes
    {
        DriveCommandConverter,
        InitDataConverter,
        VehicleStateConverter
    }
    public interface IStructConverter
    {
        public (IStructConvertable ConvStruct, byte MessageId) ConvertStruct(IStructConvertable structIn);
    }
}
