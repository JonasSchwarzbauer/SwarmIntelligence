using SwarmIntelligence.Logic.DriveControl.EventArgs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SwarmIntelligence.Logic.DriveControl.OutOfUse
{
    /// <summary>
    /// Base mailbox implementation that provides a bounded queue and a background worker
    /// to process <see cref="DriveCommand"/> instances for a single agent.
    /// </summary>
    public abstract class AbstractMailbox : IDisposable
    {
        private readonly byte agentId;
        private readonly ConcurrentQueue<DriveCommand> queue;
        private readonly SemaphoreSlim signal;
        private readonly CancellationTokenSource cts;
        
        private readonly int capacity;
        private DriveCommand? currentCommand;

        private Task? workerTask;

        /// <summary>
        /// Gets a value indicating whether the background worker that processes commands is running.
        /// </summary>
        public bool IsRunning => workerTask != null && !(workerTask.IsCompleted);

        /// <summary>
        /// Gets the agent identifier associated with this mailbox.
        /// </summary>
        public byte AgentId => agentId;

        /// <summary>
        /// Gets the maximum capacity of the mailbox queue.
        /// </summary>
        public int Capacity => capacity;

        /// <summary>
        /// Gets the number of pending commands currently in the mailbox.
        /// </summary>
        public int PendingCommandsCount => queue.Count;

        /// <summary>
        /// Gets a snapshot of all pending commands in the queue.
        /// </summary>
        public IReadOnlyCollection<DriveCommand> PendingCommands => [.. queue];

        /// <summary>
        /// Gets the command currently being processed, or null if no command is active.
        /// </summary>
        public DriveCommand? CurrentCommand => currentCommand;

        #region Events

        ///// <summary>
        ///// Occurs when a command starts processing (dequeued from the mailbox).
        ///// </summary>
        //public event EventHandler<CommandStartedEventArgs>? CommandStarted;

        ///// <summary>
        ///// Occurs when a command has been processed by the mailbox worker.
        ///// </summary>
        //public event EventHandler<CommandProcessedEventArgs>? CommandProcessed;

        /// <summary>
        /// Occurs when an error is encountered during processing or queue operations.
        /// </summary>
        public event EventHandler<CommandErrorEventArgs>? ErrorOccurred;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractMailbox"/> class.
        /// </summary>
        /// <param name="agentId">The agent identifier that owns this mailbox.</param>
        /// <param name="capacity">The bounded capacity of the mailbox queue.</param>
        public AbstractMailbox(byte agentId, int capacity)
        {
            this.agentId = agentId;
            this.capacity = capacity;
            
            queue = new ConcurrentQueue<DriveCommand>();
            signal = new SemaphoreSlim(0);
            cts = new CancellationTokenSource();
        }

        /// <summary>
        /// Starts the background worker that reads commands from the queue and processes them.
        /// Calling <see cref="Start"/> when a worker is already running has no effect.
        /// </summary>
        public void Start()
        {
            if (IsRunning) return;
            workerTask = Task.Run(RunAsync);
        }

        /// <summary>
        /// Signals the mailbox to stop accepting new commands and waits for the worker to complete.
        /// </summary>
        /// <returns>A task that completes when the background worker has finished processing pending commands.</returns>
        public async Task StopAsync()
        {
            // Cancel the wait handle to stop the loop
            cts.Cancel();
            
            if (workerTask != null)
            {
                try
                {
                    await workerTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Expected during shutdown
                }
                workerTask = null;
            }
        }

        /// <summary>
        /// Enqueues a drive command for asynchronous processing by the mailbox worker.
        /// </summary>
        /// <param name="command">The command to enqueue.</param>
        /// <returns>A <see cref="ValueTask"/> that completes when the command is written to the queue.</returns>
        public ValueTask EnqueueCommandAsync(DriveCommand command)
        {
            queue.Enqueue(command);

            // Remove oldest item if capacity exceeded
            if (queue.Count > capacity)
            {
                queue.TryDequeue(out _);
            }

            // Signal that an item is available
            signal.Release();
            
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// The background processing loop that reads commands from the queue and invokes
        /// <see cref="ProcessCommand(DriveCommand)"/> for each item.
        /// </summary>
        private async Task RunAsync()
        {
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    // Wait for an item to be signaled
                    await signal.WaitAsync(cts.Token).ConfigureAwait(false);

                    if (queue.TryDequeue(out var command))
                    {
                        try
                        {
                            // Set as current and fire CommandStarted event
                            currentCommand = command;
                            CommandStarted?.Invoke(this, new CommandStartedEventArgs(command, queue.Count, capacity));

                            // Process the command
                            await ProcessCommand(command);

                            // Fire CommandProcessed event for completion
                            CommandProcessed?.Invoke(this, new CommandProcessedEventArgs(command));
                        }
                        catch (Exception ex)
                        {
                            // Report error for this command and continue processing subsequent commands
                            ErrorOccurred?.Invoke(this, new CommandErrorEventArgs(ex, "Failed to process command in mailbox.", agentId));
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
            }
            catch (Exception ex)
            {
                // Report errors that occur while waiting/reading and exit the loop
                ErrorOccurred?.Invoke(this, new CommandErrorEventArgs(ex, "Failed when reading from queue.", agentId));
            }
        }

        /// <summary>
        /// Disposes the mailbox by cancelling the token.
        /// Use <see cref="StopAsync"/> to wait for processing to finish before disposing.
        /// </summary>
        public void Dispose()
        {
            cts.Cancel();
            cts.Dispose();
            signal.Dispose();
        }

        /// <summary>
        /// Template method invoked for each dequeued command. Derived types must implement processing logic.
        /// </summary>
        /// <param name="command">The command to process.</param>
        public abstract Task ProcessCommand(DriveCommand command);
    }
}
