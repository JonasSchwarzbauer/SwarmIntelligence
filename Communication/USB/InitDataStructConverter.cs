using SwarmIntelligence.Logic.Communication.Interfaces;
using SwarmIntelligence.Logic.Communication.USB.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwarmIntelligence.Logic.Communication
{
    public class InitDataStructConverter : IStructConverter
    {
        public (IStructConvertable ConvStruct, byte MessageId) ConvertStruct(IStructConvertable structIn)
        {
            InitData initData = (InitData)structIn;
            LeadToSlaveInitDataUSBPayload usbStruct = new();
            usbStruct.compass_offset = initData.CompassOffset;
            usbStruct.defaultTimesToMeasureDwm = initData.DefaultTimesToMeasure;
            usbStruct.anchor_positions = new float[CommunicationConfig.COORDINATE_SIZE * CommunicationConfig.NUMBER_OF_ANCHORS];
            for(int i = 0; i < CommunicationConfig.NUMBER_OF_ANCHORS; i++)
            {
                usbStruct.anchor_positions[CommunicationConfig.COORDINATE_SIZE * i] = initData.AnchorPositions[i].X;
                usbStruct.anchor_positions[CommunicationConfig.COORDINATE_SIZE * i + 1] = initData.AnchorPositions[i].Y;
            }
            usbStruct.requiredSlavesAmount = initData.RequiredSlavesAmount;
            return (usbStruct, (byte)MessageStructIDsUSB.LeadSlaveInitData);
        }
    }
}
