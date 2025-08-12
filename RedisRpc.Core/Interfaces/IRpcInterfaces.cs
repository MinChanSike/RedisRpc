namespace RedisRpc.Core.Interfaces;

/// <summary>
/// Interface for handling RPC method invocations.
/// Implement this interface to define the business logic for your RPC methods.
/// </summary>
public interface IRpcMethodHandler {
    /// <summary>
    /// Gets the collection of method names that this handler can process.
    /// </summary>
    IEnumerable<string> SupportedMethods { get; }

    /// <summary>
    /// Handles an RPC method invocation asynchronously.
    /// </summary>
    /// <param name="method">The name of the method to invoke.</param>
    /// <param name="parameters">The parameters for the method invocation.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The result of the method invocation.</returns>
    /// <exception cref="Exceptions.MethodNotFoundException">Thrown when the method is not supported.</exception>
    /// <exception cref="Exceptions.InvalidParametersException">Thrown when the parameters are invalid.</exception>
    Task<object?> HandleMethodAsync(string method, object? parameters, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for RPC client operations.
/// Use this to send RPC requests to remote services.
/// </summary>
public interface IRpcClient : IDisposable {
    /// <summary>
    /// Sends an RPC request and waits for a response.
    /// </summary>
    /// <typeparam name="T">The expected response type.</typeparam>
    /// <param name="channel">The Redis channel to send the request to.</param>
    /// <param name="method">The method name to invoke.</param>
    /// <param name="parameters">The parameters for the method.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds (overrides default).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The deserialized response.</returns>
    Task<T?> SendRequestAsync<T>(string channel, string method, object? parameters = null, int? timeoutMs = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an RPC request and waits for a response without type specification.
    /// </summary>
    /// <param name="channel">The Redis channel to send the request to.</param>
    /// <param name="method">The method name to invoke.</param>
    /// <param name="parameters">The parameters for the method.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds (overrides default).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The raw response object.</returns>
    Task<object?> SendRequestAsync(string channel, string method, object? parameters = null, int? timeoutMs = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a fire-and-forget RPC request without waiting for a response.
    /// </summary>
    /// <param name="channel">The Redis channel to send the request to.</param>
    /// <param name="method">The method name to invoke.</param>
    /// <param name="parameters">The parameters for the method.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    Task SendNotificationAsync(string channel, string method, object? parameters = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for RPC server operations.
/// Use this to listen for and handle incoming RPC requests.
/// </summary>
public interface IRpcServer : IDisposable {
    /// <summary>
    /// Registers a method handler for processing RPC requests.
    /// </summary>
    /// <param name="handler">The handler to register.</param>
    void RegisterHandler(IRpcMethodHandler handler);

    /// <summary>
    /// Starts listening for RPC requests on the specified channel.
    /// </summary>
    /// <param name="channel">The Redis channel to listen on.</param>
    /// <param name="cancellationToken">Cancellation token to stop listening.</param>
    Task StartListeningAsync(string channel, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts listening for RPC requests on multiple channels.
    /// </summary>
    /// <param name="channels">The Redis channels to listen on.</param>
    /// <param name="cancellationToken">Cancellation token to stop listening.</param>
    Task StartListeningAsync(IEnumerable<string> channels, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops listening on all channels.
    /// </summary>
    Task StopListeningAsync();
}
