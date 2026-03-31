using SwarmIntelligence.Logic.DriveControl.EventArgs;
using SwarmIntelligence.Logic.DriveControl;
using System;
using System.Linq;
using SwarmIntelligence.Logic.Communication.DTOs.DriveControl;
using SwarmIntelligence.Logic.Communication.DTOs;

namespace SwarmIntelligence.Logic.Communication
{
    public static class DriveControlEventsDtoExtensions
    {
        extension (DriveCommand command)
        {
            public DriveCommandDto ToDto() => new()
            {
                AgentId = command.AgentId,
                DriveFlags = Enum.GetValues<DriveFlags>()
                                 .Where(flag => command.DriveFlags.HasFlag(flag))
                                 .Select(flag => flag.ToString())
                                 .ToList(),
                Waypoints = command.Waypoints?.Select(wp => wp.ToDto()).ToArray() ?? [],
                Timestamp = command.TimestampCreated
            };
        }

        extension (Waypoint waypoint)
        {
            public WaypointDto ToDto() => new()
            {
                X = waypoint.X,
                Y = waypoint.Y,
                Heading = waypoint.Heading,
                MaxSpeed = waypoint.MaxSpeed
            };
        }

        extension (CommandGeneratedEventArgs args)
        {
            public CommandGeneratedDto ToDto() => new()
            {
                Command = args.Command.ToDto(),
                GeneratedAt = args.GeneratedAt,
                AgentId = args.AgentId
            };
        }

        extension (CommandAssignedEventArgs args)
        {
            public CommandAssignedDto ToDto() => new()
            {
                Command = args.Command.ToDto(),
                AgentId = args.AgentId,
                AssignedAt = args.AssignedAt
            };
        }

        extension (CommandClearedEventArgs args)
        {
            public CommandClearedDto ToDto() => new()
            {
                AgentId = args.AgentId,
                Timestamp = DateTime.UtcNow
            };
        }

        extension (CommandDispatchedEventArgs args)
        {
            public CommandDispatchedDto ToDto() => new()
            {
                Command = args.Command.ToDto(),
                AgentId = args.AgentId,
                DispatchLatencyMs = args.DispatchLatency.TotalMilliseconds,
                DispatchedAt = args.DispatchedAt
            };
        }

        extension (AgentRegistrationEventArgs args)
        {
            public AgentRegistrationDto ToDto() => new()
            {
                AgentId = args.Id,
                Timestamp = args.Timestamp,
                RegistrationType = args.Type.ToString()
            };
        }

        extension (ManagerStateChangedEventArgs args)
        {
            public ManagerStateDto ToDto() => new()
            {
                IsRunning = args.IsRunning,
                Timestamp = args.Timestamp
            };
        }

        extension (CommandErrorEventArgs args)
        {
            public DriveControlErrorDto ToDto() => new()
            {
                AgentId = args.AgentId,
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
