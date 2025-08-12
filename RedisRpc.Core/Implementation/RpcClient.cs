using RedisRpc.Core.Exceptions;
using RedisRpc.Core.Infrastructure;
using RedisRpc.Core.Interfaces;
using RedisRpc.Core.Models;
using StackExchange.Redis;
using System.Collections.Concurrent; 

namespace RedisRpc.Core.Implementation;

/// <summary>
/// Redis-based RPC client implementation that supports request/response pattern
/// and fire-and-forget notifications via Redis Pub/Sub.
/// Thread-safe and supports multiple concurrent requests.
/// </summary>
public class RpcClient : IRpcClient {
    private readonly RedisConnectionManager _connectionManager;
    private readonly RedisRpcOptions _options;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<RpcResponse>> _pendingRequests;
    private readonly string _responseChannel;
    private readonly SemaphoreSlim _connectionSemaphore;
    private volatile bool _disposed = false;
    private volatile bool _isListening = false;

    /// <summary>
    /// Initializes a new RPC client with the specified options.
    /// </summary>
    /// <param name="options">Configuration options for the RPC client.</param>
    public RpcClient(RedisRpcOptions options) {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _connectionManager = new RedisConnectionManager(options);
        _pendingRequests = new ConcurrentDictionary<string, TaskCompletionSource<RpcResponse>>();
        _responseChannel = $"{_options.ChannelPrefix}:response:{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";
        _connectionSemaphore = new SemaphoreSlim(1, 1);
    }

    /// <summary>
    /// Sends an RPC request and waits for a typed response.
    /// </summary>
    public async Task<T?> SendRequestAsync<T>(string channel, string method, object? parameters = null,
        int? timeoutMs = null, CancellationToken cancellationToken = default) {
        var result = await SendRequestAsync(channel, method, parameters, timeoutMs, cancellationToken);

        if (result == null) return default(T);

        try {
            // Handle JsonElement results (from System.Text.Json deserialization)
            if (result is System.Text.Json.JsonElement jsonElement) {
                return JsonSerializer.ConvertJsonElementToTargetType<T>(jsonElement);
            }

            // Handle primitive types and strings directly
            if (typeof(T).IsPrimitive || typeof(T) == typeof(string)) {
                return (T)Convert.ChangeType(result, typeof(T));
            }

            // For complex types, serialize and deserialize to ensure proper type conversion
            var json = JsonSerializer.Serialize(result);
            return JsonSerializer.Deserialize<T>(json);
        } catch (Exception ex) {
            throw new SerializationException($"Failed to convert response to type {typeof(T).Name}", ex);
        }
    }

    /// <summary>
    /// Sends an RPC request and waits for an untyped response.
    /// </summary>
    public async Task<object?> SendRequestAsync(string channel, string method, object? parameters = null,
        int? timeoutMs = null, CancellationToken cancellationToken = default) {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(channel))
            throw new ArgumentException("Channel cannot be null or empty", nameof(channel));

        if (string.IsNullOrWhiteSpace(method))
            throw new ArgumentException("Method cannot be null or empty", nameof(method));

        await EnsureListeningAsync(cancellationToken);

        var request = new RpcRequest {
            Method = method,
            Parameters = parameters,
            ResponseChannel = _responseChannel,
            TimeoutMs = timeoutMs ?? _options.DefaultTimeoutMs
        };

        var tcs = new TaskCompletionSource<RpcResponse>();
        _pendingRequests[request.Id] = tcs;

        try {
            // Serialize and publish the request
            var requestJson = JsonSerializer.Serialize(request);
            var fullChannel = $"{_options.ChannelPrefix}:request:{channel}";

            await _connectionManager.Subscriber.PublishAsync(fullChannel, requestJson);

            // Wait for response with timeout
            var timeout = TimeSpan.FromMilliseconds(timeoutMs ?? _options.DefaultTimeoutMs);
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try {
                var response = await tcs.Task.WaitAsync(combinedCts.Token);

                if (!response.Success) {
                    var error = response.Error!;
                    throw CreateExceptionFromError(error);
                }

                return response.Result;
            } catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested) {
                throw new RpcTimeoutException(timeoutMs ?? _options.DefaultTimeoutMs);
            }
        } finally {
            _pendingRequests.TryRemove(request.Id, out _);
        }
    }

    /// <summary>
    /// Sends a fire-and-forget notification (no response expected).
    /// </summary>
    public async Task SendNotificationAsync(string channel, string method, object? parameters = null,
        CancellationToken cancellationToken = default) {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(channel))
            throw new ArgumentException("Channel cannot be null or empty", nameof(channel));

        if (string.IsNullOrWhiteSpace(method))
            throw new ArgumentException("Method cannot be null or empty", nameof(method));

        var request = new RpcRequest {
            Method = method,
            Parameters = parameters,
            ResponseChannel = string.Empty // No response channel for notifications
        };

        var requestJson = JsonSerializer.Serialize(request);
        var fullChannel = $"{_options.ChannelPrefix}:request:{channel}";

        await _connectionManager.Subscriber.PublishAsync(fullChannel, requestJson);
    }

    /// <summary>
    /// Ensures the client is listening for responses on its response channel.
    /// </summary>
    private async Task EnsureListeningAsync(CancellationToken cancellationToken = default) {
        if (_isListening || _disposed)
            return;

        await _connectionSemaphore.WaitAsync(cancellationToken);
        try {
            if (_isListening || _disposed)
                return;

            await _connectionManager.Subscriber.SubscribeAsync(_responseChannel, OnResponseReceived);
            _isListening = true;
        } finally {
            _connectionSemaphore.Release();
        }
    }

    /// <summary>
    /// Handles incoming response messages.
    /// </summary>
    private void OnResponseReceived(RedisChannel channel, RedisValue message) {
        try {
            if (!message.HasValue) return;

            var response = JsonSerializer.Deserialize<RpcResponse>(message);
            if (response == null) return;

            if (_pendingRequests.TryGetValue(response.Id, out var tcs)) {
                tcs.SetResult(response);
            }
        } catch (Exception ex) {
            // Log the error (in a real application, you'd use a proper logger)
            Console.WriteLine($"Error processing response: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates an appropriate exception from an RPC error.
    /// </summary>
    private static Exception CreateExceptionFromError(RpcError error) {
        return error.Code switch {
            RpcErrorCode.MethodNotFound => new MethodNotFoundException(error.Details?.ToString() ?? "Unknown method"),
            RpcErrorCode.InvalidParameters => new InvalidParametersException(error.Message, error.Details),
            RpcErrorCode.Timeout => new RpcTimeoutException(0), // Timeout already occurred on server
            RpcErrorCode.SerializationError => new SerializationException(error.Message, new Exception(error.Details?.ToString())),
            RpcErrorCode.ConnectionError => new ConnectionException(error.Message, new Exception(error.Details?.ToString())),
            RpcErrorCode.InternalError => new InternalRpcException(error.Message),
            _ => new InternalRpcException(error.Message ?? "Unknown error occurred")
        };
    }

    /// <summary>
    /// Throws ObjectDisposedException if the client has been disposed.
    /// </summary>
    private void ThrowIfDisposed() {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RpcClient));
    }

    /// <summary>
    /// Disposes the RPC client and all associated resources.
    /// </summary>
    public void Dispose() {
        if (_disposed)
            return;

        _disposed = true;

        // Complete all pending requests with cancellation
        foreach (var kvp in _pendingRequests) {
            kvp.Value.TrySetCanceled();
        }
        _pendingRequests.Clear();

        // Unsubscribe from response channel
        if (_isListening) {
            try {
                _connectionManager.Subscriber.Unsubscribe(_responseChannel);
            } catch (Exception ex) {
                Console.WriteLine($"Error unsubscribing from response channel: {ex.Message}");
            }
        }

        _connectionSemaphore?.Dispose();
        _connectionManager?.Dispose();
    }
}
