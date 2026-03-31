using System.Threading;
using SwarmIntelligence.Logic.MapControl.EventArgs;

namespace SwarmIntelligence.Logic.MapControl.Interfaces
{
    public interface IDataProvider
    {
        void StartProviding(CancellationToken ct); // needs to put data into VehicleDataBuffer
        event EventHandler<UsbStartedEventArgs>? StartedUSB;
        event EventHandler<BufferInformationEventArgs>? BufferInformationEvent; // to monitor buffer status
    }
}
