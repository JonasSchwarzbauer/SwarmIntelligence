using SwarmIntelligence.Logic.Communication.Factories;
using SwarmIntelligence.Logic.Communication.Interfaces;
using SwarmIntelligence.Logic.Communication.USB.Interfaces;
using SwarmIntelligence.Logic.MapControl;
using SwarmIntelligence.Logic.MapControl.EventArgs;
using System;
using System.Collections.Generic;
using System.Text;

namespace SwarmIntelligence.Logic.Communication.USB
{
    // Implement as singleton
    public class USBReceiver()
    {
        private static readonly Lazy<USBReceiver> _instance = new Lazy<USBReceiver>(() => new USBReceiver());
        private static int Capacity => VehicleDataBuffer.Capacity;
        
        public static event EventHandler<BufferInformationEventArgs>? BufferInformationEvent;
        public static USBReceiver Instance => _instance.Value;
        private readonly Lazy<VehicleStateStructConverter> vehicleStateConverter = new Lazy<VehicleStateStructConverter>(() => (VehicleStateStructConverter)StructConverterFactory.CreateStructConverter(StructConverterTypes.VehicleStateConverter));
        
        public void ProcessReceivedStruct(IUSBReceiveStruct received)
        {
            Type structType = received.GetType();

            if (structType == typeof(SlaveToLeadVehicleStateUSBPayload))
            {
                var converted = vehicleStateConverter.Value.ConvertStruct(received);

                // Process vehicle state into buffer channel
                bool success = VehicleDataBuffer.ProcessVehicleState((AgentGeoData)converted.ConvStruct);

                // Fire event with buffer information
                BufferInformationEvent?.Invoke(this, new BufferInformationEventArgs(success, VehicleDataBuffer.CurrentBufferCount, Capacity));
            }
        }
    }
}