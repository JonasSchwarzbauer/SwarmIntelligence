using SwarmIntelligence.Logic.Communication.USB;
using SwarmIntelligence.Logic.MapControl.EventArgs;
using SwarmIntelligence.Logic.MapControl.Interfaces;
using System.Threading.Channels;

namespace SwarmIntelligence.Logic.MapControl
{
    public class MapWorker : IMapWorker
    {
        private CancellationTokenSource? _cts;
        private Task? _workerTask;
        private IMapUpdateCoordinator? _updateCoordinator;

        public bool IsRunning => _workerTask != null;

        public event EventHandler<MapErrorEventArgs>? ErrorOccurred;
        public event EventHandler<WorkerStateChangedEventArgs>? WorkerStateChanged;
        public event EventHandler<BufferInformationEventArgs>? BufferInformationEvent;

        public Task StartAsync(IMapUpdateCoordinator updateCoordinator, CancellationToken ct = default)
        {
            if (_workerTask != null) return Task.CompletedTask;

            _updateCoordinator = updateCoordinator ?? throw new ArgumentNullException(nameof(updateCoordinator));

            // Clear any stale data from the buffer before starting
            // VehicleDataBuffer.Clear();

            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _workerTask = Task.Run(() => RunAsync(_cts.Token), CancellationToken.None);

            WorkerStateChanged?.Invoke(this, new WorkerStateChangedEventArgs(WorkerState.Started));

            return Task.CompletedTask;
        }


        public async Task StopAsync()
        {
            if (_cts == null) return;

            try
            {
                _cts.Cancel();
                if (_workerTask != null)
                {
                    await _workerTask.ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (_cts.IsCancellationRequested)
            {
                // Expected when cancelling
            }
            catch (Exception ex)
            {
                // Log or handle unexpected exceptions
                ErrorOccurred?.Invoke(this, new MapErrorEventArgs(ex, "Failed to stop map worker."));
            }
            finally
            {
                _cts.Dispose();
                _cts = null;
                _workerTask = null;
                _updateCoordinator = null;

                WorkerStateChanged?.Invoke(this, new WorkerStateChangedEventArgs(WorkerState.Stopped));
            }
        }

        private async Task RunAsync(CancellationToken ct)
        {
            ChannelReader<AgentGeoData> reader = VehicleDataBuffer.GetReader();

            try
            {
                await foreach (var vehicleState in reader.ReadAllAsync(ct).ConfigureAwait(false))
                {
                    try
                    {
                        _updateCoordinator!.Update(vehicleState);
                        BufferInformationEvent?.Invoke(this, new BufferInformationEventArgs(true, VehicleDataBuffer.CurrentBufferCount, VehicleDataBuffer.Capacity));

                        // Debug for Agent 170
                        if (vehicleState.AgentId == 170)
                        {
                            Console.WriteLine($"Agent 170 - Position: {vehicleState.Position}, FrontalDistance: {vehicleState.FrontalDistance}, Orientation: {vehicleState.Orientation}");
                            throw new InvalidDataException("Agent 170 sent data");
                        }
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        // Expected when cancelling — state published by StopAsync
                        break;
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        ErrorOccurred?.Invoke(this, new MapErrorEventArgs(ex, $"Error converting position to grid coordinates."));
                    }
                    catch (InvalidDataException ex)
                    {
                        ErrorOccurred?.Invoke(this, new MapErrorEventArgs(ex, $"Received data from Agent 170: {vehicleState.AgentId}."));
                    }
                    catch (Exception ex)
                    {
                        // Log or handle unexpected exceptions
                        ErrorOccurred?.Invoke(this, new MapErrorEventArgs(ex, "Unexpected exception occurred while updating with coordinator."));
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Expected when cancelling — state published by StopAsync
            }
            catch (Exception ex)
            {
                // Log or handle unexpected exceptions
                ErrorOccurred?.Invoke(this, new MapErrorEventArgs(ex, "Unexpected exception occurred while reading vehicle state from buffer."));
            }
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync().ConfigureAwait(false);
        }
    }
}
