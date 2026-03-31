using SwarmIntelligence.Logic.DriveControl.EventArgs;
using SwarmIntelligence.Logic.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwarmIntelligence.Logic.DriveControl.Interfaces
{
    public class DriveCommandGenerator : IDriveCommandGenerator
    {
        public event EventHandler<CommandGeneratedEventArgs>? CommandGenerated;

        public DriveCommand GenerateCommand(NavResult navResult)
        {
            var handler = CommandGenerated;

            DriveCommand command = new DriveCommand
            {
                AgentId = navResult.AgentId,
                DriveFlags = navResult.DriveFlags,
                Waypoints = navResult.Waypoints,
                TimestampCreated = DateTime.UtcNow
            };

            handler?.Invoke(this, new CommandGeneratedEventArgs(command));
            return command;
        }
    }
}