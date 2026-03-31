using SwarmIntelligence.Logic.Communication.DTOs;
using SwarmIntelligence.Logic.Communication.DTOs.MapControl;
using SwarmIntelligence.Logic.Communication.USB;
using SwarmIntelligence.Logic.MapControl;
using SwarmIntelligence.Logic.MapControl.EventArgs;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace SwarmIntelligence.Logic.Communication
{
    public static class MapControlEventsDtoExtensions
    {
        extension (AgentGeoData data)
        {
            public AgentDataDto ToDto() => new()
            {
                AgentId = data.AgentId,
                X = data.Position.X,
                Y = data.Position.Y,
                Orientation = data.Orientation,
                Velocity = data.Velocity,
                FrontalDistance = data.FrontalDistance,
                Target = data.Target.ToDto(),
                Flags = Enum.GetValues<VehicleFlags>()
                .Where(flag => data.Flags.HasFlag(flag))
                .Select(flag => flag.ToString())
                .ToList(),
                DwmSuccessRate = data.DwmSuccessRate,
                DataReceived = data.DataReceived,
                Timestamp = DateTime.UtcNow
            };
        }

        extension (AgentDataUpdatedEventArgs args)
        {
            public AgentDataDto ToDto() => args.AgentData.ToDto() with { Timestamp = args.Timestamp };
        }

        extension (BufferInformationEventArgs args)
        {
            public BufferInformationDto ToDto() => new()
            {
                Success = args.Success,
                BufferCount = args.BufferCount,
                BufferCapacity = args.BufferCapacity,
                BufferUsagePercentage = args.BufferUsagePercentage,
                Timestamp = args.Timestamp
            };
        }

        extension(MapUpdatedEventArgs args)
        {
            public MapUpdatedDto ToDto() => new()
            {
                X = args.Position.X,
                Y = args.Position.Y,
                CellX = args.Cell.Item1,
                CellY = args.Cell.Item2,
                Occupied = args.Occupied,
                Timestamp = args.Timestamp
            };
        }

        extension(WorkerStateChangedEventArgs args)
        {
            public WorkerStateDto ToDto() => new()
            {
                State = args.State.ToString(),
                Timestamp = args.Timestamp
            };
        }

        extension(UsbStartedEventArgs args)
        {
            public  UsbStartedDto ToDto() => new()
            {
                Success = args.Success,
                PortName = args.PortName,
                BaudRate = args.BaudRate,
                Message = args.Message,
                Timestamp = args.Timestamp
            };
        }

        extension(MapErrorEventArgs args)
        {
            public MapControlErrorDto ToDto() => new()
            {
                Message = args.Message,
                Source = args.Source,
                SourceContext = args.SourceContext,
                ExceptionMessage = args.ExceptionMessage,
                Exception = args.GetException(),
                Timestamp = args.Timestamp
            };
        }
    }
}
