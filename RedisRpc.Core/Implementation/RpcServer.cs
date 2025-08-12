using RedisRpc.Core.Exceptions;
using RedisRpc.Core.Infrastructure;
using RedisRpc.Core.Interfaces;
using RedisRpc.Core.Models;
using StackExchange.Redis;
using System.Collections.Concurrent;

namespace RedisRpc.Core.Implementation;

/// <summary>
/// Redis-based RPC server implementation that listens for incoming requests
/// and processes them using registered method handlers.
/// Supports multiple channels and concurrent request processing.
/// </summary>
public class RpcServer : IRpcServer {
    private readonly RedisConnectionManager _connectionManager;
    private readonly RedisRpcOptions _options;
    private readonly ConcurrentDictionary<string, IRpcMethodHandler> _handlers;
    private readonly ConcurrentDictionary<string, bool> _listeningChannels;
    private readonly SemaphoreSlim _requestSemaphore;
    private volatile bool _disposed = false;

    /// <summary>
    /// Initializes a new RPC server with the specified options.
    /// </summary>
    /// <param name="options">Configuration options for the RPC server.</param>
    public RpcServer(RedisRpcOptions options) {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _connectionManager = new RedisConnectionManager(options);
        _handlers = new ConcurrentDictionary<string, IRpcMethodHandler>();
        _listeningChannels = new ConcurrentDictionary<string, bool>();
        _requestSemaphore = new SemaphoreSlim(_options.MaxConcurrentRequests, _options.MaxConcurrentRequests);
    }

    /// <summary>
    /// Registers a method handler for processing RPC requests.
    /// </summary>
    public void RegisterHandler(IRpcMethodHandler handler) {
        ThrowIfDisposed();

        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        foreach (var method in handler.SupportedMethods) {
            if (string.IsNullOrWhiteSpace(method))
                continue;

            _handlers.AddOrUpdate(method, handler, (key, existing) => handler);
        }
    }

    /// <summary>
    /// Starts listening for RPC requests on the specified channel.
    /// </summary>
    public async Task StartListeningAsync(string channel, CancellationToken cancellationToken = default) {
        await StartListeningAsync(new[] { channel }, cancellationToken);
    }

    /// <summary>
    /// Starts listening for RPC requests on multiple channels.
    /// </summary>
    public async Task StartListeningAsync(IEnumerable<string> channels, CancellationToken cancellationToken = default) {
        ThrowIfDisposed();

        if (channels == null)
            throw new ArgumentNullException(nameof(channels));

        var channelList = channels.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
        if (channelList.Count == 0)
            throw new ArgumentException("At least one valid channel must be provided", nameof(channels));

        var tasks = new List<Task>();

        foreach (var channel in channelList) {
            if (_listeningChannels.ContainsKey(channel))
                continue; // Already listening on this channel

            var fullChannel = $"{_options.ChannelPrefix}:request:{channel}";
            _listeningChannels[channel] = true;

            tasks.Add(SubscribeToChannelAsync(fullChannel, cancellationToken));
        }

        if (tasks.Count > 0) {
            await Task.WhenAll(tasks);
        }
    }

    /// <summary>
    /// Stops listening on all channels.
    /// </summary>
    public async Task StopListeningAsync() {
        if (_disposed)
            return;

        var channels = _listeningChannels.Keys.ToList();
        _listeningChannels.Clear();

        var tasks = channels.Select(channel =>
            UnsubscribeFromChannelAsync($"{_options.ChannelPrefix}:request:{channel}"));

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Subscribes to a specific Redis channel.
    /// </summary>
    private async Task SubscribeToChannelAsync(string fullChannel, CancellationToken cancellationToken) {
        try {
            await _connectionManager.Subscriber.SubscribeAsync(fullChannel, OnRequestReceived);
            Console.WriteLine($"RPC server started listening on channel: {fullChannel}");
        } catch (Exception ex) {
            Console.WriteLine($"Failed to subscribe to channel {fullChannel}: {ex.Message}");
            throw new ConnectionException($"Failed to subscribe to channel {fullChannel}", ex);
        }
    }

    /// <summary>
    /// Unsubscribes from a specific Redis channel.
    /// </summary>
    private async Task UnsubscribeFromChannelAsync(string fullChannel) {
        try {
            await _connectionManager.Subscriber.UnsubscribeAsync(fullChannel);
            Console.WriteLine($"RPC server stopped listening on channel: {fullChannel}");
        } catch (Exception ex) {
            Console.WriteLine($"Error unsubscribing from channel {fullChannel}: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles incoming RPC request messages.
    /// </summary>
    private async void OnRequestReceived(RedisChannel channel, RedisValue message) {
        if (!message.HasValue || _disposed)
            return;

        // Fire and forget - don't block the Redis subscriber
        _ = Task.Run(() => ProcessRequestAsync(message));
    }

    /// <summary>
    /// Processes an individual RPC request.
    /// </summary>
    private async Task ProcessRequestAsync(RedisValue message) {
        await _requestSemaphore.WaitAsync();

        try {
            var request = JsonSerializer.Deserialize<RpcRequest>(message);
            if (request == null) return;

            // If no response channel is specified, this is a notification (fire-and-forget)
            var isNotification = string.IsNullOrWhiteSpace(request.ResponseChannel);

            RpcResponse? response = null;

            try {
                // Find and execute the handler
                var result = await ExecuteMethodAsync(request.Method, request.Parameters);

                if (!isNotification) {
                    response = new RpcResponse {
                        Id = request.Id,
                        Success = true,
                        Result = result
                    };
                }
            } catch (Exception ex) {
                if (!isNotification) {
                    response = new RpcResponse {
                        Id = request.Id,
                        Success = false,
                        Error = CreateRpcError(ex)
                    };
                } else {
                    // For notifications, log the error but don't send a response
                    Console.WriteLine($"Error processing notification for method '{request.Method}': {ex.Message}");
                }
            }

            // Send response if this is not a notification
            if (response != null && !isNotification) {
                await SendResponseAsync(request.ResponseChannel, response);
            }
        } catch (Exception ex) {
            Console.WriteLine($"Critical error processing RPC request: {ex.Message}");
        } finally {
            _requestSemaphore.Release();
        }
    }

    /// <summary>
    /// Executes the specified method using the registered handlers.
    /// </summary>
    private async Task<object?> ExecuteMethodAsync(string method, object? parameters) {
        if (!_handlers.TryGetValue(method, out var handler)) {
            throw new MethodNotFoundException(method);
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_options.DefaultTimeoutMs));
        return await handler.HandleMethodAsync(method, parameters, cts.Token);
    }

    /// <summary>
    /// Sends a response back to the requesting client.
    /// </summary>
    private async Task SendResponseAsync(string responseChannel, RpcResponse response) {
        try {
            var responseJson = JsonSerializer.Serialize(response);
            await _connectionManager.Subscriber.PublishAsync(responseChannel, responseJson);
        } catch (Exception ex) {
            Console.WriteLine($"Failed to send RPC response: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates an RpcError from an exception.
    /// </summary>
    private RpcError CreateRpcError(Exception exception) {
        return exception switch {
            RpcException rpcEx => rpcEx.ToRpcError(_options.IncludeStackTraceInErrors),
            _ => new RpcError {
                Code = RpcErrorCode.InternalError,
                Message = exception.Message,
                Details = exception.GetType().Name,
                StackTrace = _options.IncludeStackTraceInErrors ? exception.StackTrace : null
            }
        };
    }

    /// <summary>
    /// Throws ObjectDisposedException if the server has been disposed.
    /// </summary>
    private void ThrowIfDisposed() {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RpcServer));
    }

    /// <summary>
    /// Disposes the RPC server and all associated resources.
    /// </summary>
    public void Dispose() {
        if (_disposed)
            return;

        _disposed = true;

        // Stop listening on all channels
        try {
            StopListeningAsync().GetAwaiter().GetResult();
        } catch (Exception ex) {
            Console.WriteLine($"Error stopping RPC server: {ex.Message}");
        }

        _requestSemaphore?.Dispose();
        _connectionManager?.Dispose();
    }
}
