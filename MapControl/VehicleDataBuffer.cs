using SwarmIntelligence.Logic.Communication.USB;
using SwarmIntelligence.Logic.Communication.USB.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Channels;

namespace SwarmIntelligence.Logic.MapControl
{
    public static class VehicleDataBuffer
    {
        private static readonly int capacity = 25;
        private static Channel<AgentGeoData> _channel = Channel.CreateBounded<AgentGeoData>(new BoundedChannelOptions(capacity)
        {
            SingleReader = true,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.DropOldest
        });

        public static int CurrentBufferCount => _channel.Reader.Count;

        public static int Capacity => capacity;

        public static bool ProcessVehicleState(AgentGeoData state) => _channel.Writer.TryWrite(state);

        public static void Close() => _channel.Writer.Complete();

        public static void Clear()
        {
            while (_channel.Reader.TryRead(out _)) { }
        }

        public static ChannelReader<AgentGeoData> GetReader() => _channel.Reader;
    }
}
