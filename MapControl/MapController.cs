using SwarmIntelligence.Logic.Communication.DTOs.UserInputs;
using SwarmIntelligence.Logic.Communication.USB;
using SwarmIntelligence.Logic.MapControl.EventArgs;
using SwarmIntelligence.Logic.MapControl.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace SwarmIntelligence.Logic.MapControl
{
    public class MapController : IAsyncDisposable
    {
        private readonly IDataProvider _dataProvider;
        private readonly IMapUpdateCoordinator _updateCoordinator;
        private readonly IMapQueryService _queryService;
        private readonly IMapWorker _mapWorker;

        private CancellationTokenSource? _cts;
        private bool _disposed;

        #region Eventhandlers

        public event EventHandler<MapUpdatedEventArgs>? MapUpdated;

        public event EventHandler<AgentDataUpdatedEventArgs>? AgentStateUpdated;

        public event EventHandler<MapErrorEventArgs>? ErrorOccurred;

        public event EventHandler<UsbStartedEventArgs>? USBStarted;

        public event EventHandler<BufferInformationEventArgs>? BufferInformationEvent;

        public event EventHandler<WorkerStateChangedEventArgs>? WorkerStateChanged;

        #endregion

        public MapController(
            IDataProvider dataProvider,
            IMapUpdateCoordinator updateCoordinator,
            IMapQueryService queryService,
            IMapWorker mapWorker)
        {
            _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
            _updateCoordinator = updateCoordinator ?? throw new ArgumentNullException(nameof(updateCoordinator));
            _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
            _mapWorker = mapWorker ?? throw new ArgumentNullException(nameof(mapWorker));

            // Attach event handlers
            _dataProvider.StartedUSB += OnUSBStarted;
            _dataProvider.BufferInformationEvent += OnBufferInformation;

            _updateCoordinator.MapUpdated += OnMapUpdated;
            _updateCoordinator.AgentStateUpdated += OnAgentStateUpdated;

            _mapWorker.ErrorOccurred += OnErrorOccurred;
            _mapWorker.WorkerStateChanged += OnWorkerStateChanged;
            _mapWorker.BufferInformationEvent += OnBufferInformation;
        }

        #region Public Query Methods

        public bool IsRunning => _mapWorker.IsRunning;

        public (int Width, int Height, float CellSize) GetMapDimensions() => (_queryService.GridWidth, _queryService.GridHeight, _queryService.CellSize);

        public bool[,] GetMapGrid() => _queryService.GetMapGrid();

        public IReadOnlyCollection<AgentGeoData> GetAllAgentData() => _queryService.GetAllAgentData();

        public AgentGeoData? GetAgentDataById(byte agentId) => _queryService.GetAgentDataById(agentId);

        public bool[,] GetObstacleGrid() => _queryService.GetObstacleGrid();

        public void UpdateGrid(ObstacleCellDto cell)
        {
            _updateCoordinator.UpdateGrid(cell);
        }

        #endregion

        #region Lifecycle Methods

        public Task Start()
        {
            if (_cts != null)
            {
                // already started
                return Task.CompletedTask;
            }

            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            // Start providing data to buffer
            _dataProvider.StartProviding(ct);

            // Start map worker to process data with update coordinator who owns the stores
            _mapWorker.StartAsync(_updateCoordinator, ct);

            return Task.CompletedTask;
        }

        public async Task Stop()
        {
            if (_cts == null) return;

            try
            {
                _cts.Cancel();
                await _mapWorker.StopAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelling
            }
            catch (Exception ex)
            {
                // Log or handle other exceptions if necessary
                ErrorOccurred?.Invoke(this, new MapErrorEventArgs(ex, "Unexpected exception occurred while stopping Map Controller."));
            }
            finally
            {
                _cts.Dispose();
                _cts = null;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            await Stop().ConfigureAwait(false);

            try
            {
                // Detach event handlers
                _dataProvider.StartedUSB -= OnUSBStarted;
                _dataProvider.BufferInformationEvent -= OnBufferInformation;
                _updateCoordinator.MapUpdated -= OnMapUpdated;
                _updateCoordinator.AgentStateUpdated -= OnAgentStateUpdated;
                _mapWorker.ErrorOccurred -= OnErrorOccurred;
                _mapWorker.WorkerStateChanged -= OnWorkerStateChanged;
                _mapWorker.BufferInformationEvent -= OnBufferInformation;
            }
            catch
            {
                // Swallow exceptions during dispose
            }
            finally
            {
                // Clear public event subscribers
                MapUpdated = null;
                AgentStateUpdated = null;
                ErrorOccurred = null;
                USBStarted = null;
                WorkerStateChanged = null;
                BufferInformationEvent = null;
            }
        }

        #endregion

        #region Private Event Invokers

        private void OnMapUpdated(object? sender, MapUpdatedEventArgs e)
        {
            var handler = MapUpdated;
            handler?.Invoke(this, e);
        }

        private void OnAgentStateUpdated(object? sender, AgentDataUpdatedEventArgs e)
        {
            var handler = AgentStateUpdated;
            handler?.Invoke(this, e);
        }

        private void OnErrorOccurred(object? sender, MapErrorEventArgs e)
        {
            var handler = ErrorOccurred;
            handler?.Invoke(this, e);
        }

        private void OnUSBStarted(object? sender, UsbStartedEventArgs e)
        {
            var handler = USBStarted;
            handler?.Invoke(this, e);
        }

        private void OnBufferInformation(object? sender, BufferInformationEventArgs e)
        {
            var handler = BufferInformationEvent;
            handler?.Invoke(this, e);
        }

        private void OnWorkerStateChanged(object? sender, WorkerStateChangedEventArgs e)
        {
            var handler = WorkerStateChanged;
            handler?.Invoke(this, e);
        }

        #endregion
    }
}
