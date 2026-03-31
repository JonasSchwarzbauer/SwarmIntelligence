using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SwarmIntelligence.Logic.Communication;
using SwarmIntelligence.Logic.Communication.DTOs;
using SwarmIntelligence.Logic.Setup;
using System;
using System.Threading.Tasks;

namespace SwarmIntelligence.Logic.Communication
{
    /// <summary>
    /// Handles communication with a remote webserver SignalR hub.
    /// </summary>
    /// <remarks>
    /// This class wraps a <see cref="HubConnection"/> instance and provides
    /// convenience methods to start/stop the connection and to send domain DTOs
    /// to the server. The default hub URL is <c>http://127.0.0.1:5042/datahub</c>
    /// but a custom URL can be provided via the constructor.
    /// </remarks>
    public abstract class WebserverCommunicationHandler : IAsyncDisposable, IDisposable
    {
        private class HubReconnectRetryPolicy : IRetryPolicy
        {
            private static readonly TimeSpan[] Delays =
            [
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(3),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(30)
            ];

            public TimeSpan? NextRetryDelay(RetryContext retryContext)
            {
                if (retryContext.PreviousRetryCount >= Delays.Length)
                    return Delays[^1]; // keep trying every 30s forever

                return Delays[retryContext.PreviousRetryCount];
            }
        }

        private async Task<bool> WaitUntilConnectedAsync(TimeSpan timeout)
        {
            var startedAt = DateTime.UtcNow;

            while (connection.State == HubConnectionState.Connecting || connection.State == HubConnectionState.Reconnecting)
            {
                if (DateTime.UtcNow - startedAt >= timeout)
                {
                    return false;
                }

                await Task.Delay(250);
            }

            return connection.State == HubConnectionState.Connected;
        }

        private readonly ILogger<WebserverCommunicationHandler> _logger;

        /// <summary>
        /// Default URL used when no hub URL is provided to the constructor.
        /// </summary>
        private const string DefaultHubUrl = "http://127.0.0.1:5042/datahub";

        private HubConnection connection;

        /// <summary>
        /// Initializes a new instance that uses the default hub URL and a null logger.
        /// </summary>
        public WebserverCommunicationHandler() : this(NullLogger<WebserverCommunicationHandler>.Instance, DefaultHubUrl) { }

        /// <summary>
        /// Initializes a new instance that uses the default hub URL and the specified logger.
        /// </summary>
        /// <param name="logger">Logger instance to record connection lifecycle and errors.</param>
        public WebserverCommunicationHandler(ILogger<WebserverCommunicationHandler> logger) : this(logger, DefaultHubUrl) { }

        /// <summary>
        /// Initializes a new instance that connects to the specified SignalR hub URL.
        /// </summary>
        /// <param name="logger">Logger instance. If <c>null</c>, a <see cref="NullLogger"/> is used.</param>
        /// <param name="hubUrl">The full URL of the SignalR hub (e.g. <c>http://localhost:5000/datahub</c>).
        /// If <c>null</c> or whitespace, the default URL is used.</param>
        public WebserverCommunicationHandler(ILogger<WebserverCommunicationHandler> logger, string hubUrl)
        {
            _logger = logger ?? NullLogger<WebserverCommunicationHandler>.Instance;

            if (string.IsNullOrWhiteSpace(hubUrl))
            {
                hubUrl = DefaultHubUrl;
            }

            _logger.LogInformation("Creating SignalR connection to {HubUrl}", hubUrl);

            connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect(new HubReconnectRetryPolicy())
                .Build();

            // Attach lifecycle logging
            connection.Reconnecting += ex =>
            {
                _logger.LogWarning(ex, "SignalR connection is reconnecting.");
                return Task.CompletedTask;
            };

            connection.Reconnected += connectionId =>
            {
                _logger.LogInformation("SignalR connection reconnected. ConnectionId: {ConnectionId}", connectionId);
                return Task.CompletedTask;
            };

            connection.Closed += ex =>
            {
                _logger.LogWarning(ex, "SignalR connection has closed.");
                return Task.CompletedTask;
            };
        }

        /// <summary>
        /// Starts the SignalR connection if it is not already started.
        /// </summary>
        /// <returns>A task that completes when the start operation finishes (successfully or with an error logged).</returns>
        public async Task StartAsync()
        {
            switch (connection.State)
            {
                case HubConnectionState.Connected:
                    _logger.LogDebug("SignalR connection already started.");
                    return;
                case HubConnectionState.Connecting:
                case HubConnectionState.Reconnecting:
                    _logger.LogDebug("SignalR connection start/reconnect already in progress.");
                    return;
                case HubConnectionState.Disconnected:
                    try
                    {
                        await connection.StartAsync();
                        _logger.LogInformation("SignalR connection started.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error starting SignalR connection");
                    }
                    return;
                default:
                    _logger.LogWarning("Unhandled SignalR connection state during start: {State}", connection.State);
                    return;
            }
        }

        /// <summary>
        /// Stops the SignalR connection if it is running.
        /// </summary>
        /// <returns>A task that completes when the stop operation finishes (successfully or with an error logged).</returns>
        public async Task StopAsync()
        {
            switch (connection.State)
            {
                case HubConnectionState.Disconnected:
                    _logger.LogDebug("SignalR connection already stopped.");
                    return;
                case HubConnectionState.Connected:
                case HubConnectionState.Connecting:
                case HubConnectionState.Reconnecting:
                    try
                    {
                        await connection.StopAsync();
                        _logger.LogInformation("SignalR connection stopped.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error stopping SignalR connection");
                    }
                    return;
                default:
                    _logger.LogWarning("Unhandled SignalR connection state during stop: {State}", connection.State);
                    return;
            }
        }

        /// <summary>
        /// Invokes the specified hub method with the provided payload if the connection is established.
        /// </summary>
        /// <typeparam name="T">Type of the payload to send.</typeparam>
        /// <param name="methodName">The name of the hub method to invoke.</param>
        /// <param name="data">The payload to send to the hub method.</param>
        protected async Task SendDataAsync<T>(MethodNames methodName, T data)
        {
            if (connection.State == HubConnectionState.Disconnected)
            {
                _logger.LogWarning("SignalR connection is disconnected. Attempting to start connection before send.");
                await StartAsync();
            }

            if (connection.State == HubConnectionState.Connecting || connection.State == HubConnectionState.Reconnecting)
            {
                _logger.LogDebug("SignalR connection is {State}. Waiting for reconnect before send.", connection.State);
                var connected = await WaitUntilConnectedAsync(TimeSpan.FromSeconds(5));

                if (!connected)
                {
                    _logger.LogWarning("SignalR did not reconnect within timeout. Skipping send for method '{methodName}'.", methodName);
                    return;
                }
            }

            if (connection.State != HubConnectionState.Connected)
            {
                _logger.LogWarning("SignalR connection is not established. Current state: {State}. Skipping send for method '{methodName}'.", connection.State, methodName);
                return;
            }

            try
            {
                await connection.InvokeAsync(methodName.ToString(), data);
                _logger.LogDebug("Data sent via SignalR method '{methodName}'.", methodName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending data via SignalR with payload: {data}", data);
            }
        }

        /// <summary>
        /// Registers a handler to receive data from the specified hub method.
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        protected IDisposable ReceiveData<T>(MethodNames methodName, Func<T, Task> handler)
        {
            return connection.On(methodName.ToString(), handler);
        }

        protected IDisposable ReceiveData(MethodNames methodName, Func<Task> handler)
        {
            return connection.On(methodName.ToString(), handler);
        }

        /// <summary>
        /// Asynchronously disposes the underlying <see cref="HubConnection"/> instance.
        /// </summary>
        /// <returns>A <see cref="ValueTask"/> representing the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            try
            {
                if (connection.State != HubConnectionState.Disconnected)
                {
                    await connection.StopAsync();
                }

                await connection.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing SignalR connection");
            }
        }

        public void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}
