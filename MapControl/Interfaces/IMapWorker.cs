using SwarmIntelligence.Logic.Communication.USB;
using SwarmIntelligence.Logic.MapControl.EventArgs;
using System.Numerics;

namespace SwarmIntelligence.Logic.MapControl.Interfaces
{
    public interface IMapWorker : IAsyncDisposable
    {
        bool IsRunning { get; }
        Task StartAsync(IMapUpdateCoordinator updateCoordinator, CancellationToken ct = default);
        Task StopAsync();
        event EventHandler<MapErrorEventArgs> ErrorOccurred;
        public event EventHandler<WorkerStateChangedEventArgs>? WorkerStateChanged;
        public event EventHandler<BufferInformationEventArgs>? BufferInformationEvent;
    }
}