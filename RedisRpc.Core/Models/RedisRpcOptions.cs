namespace RedisRpc.Core.Models;

/// <summary>
/// Configuration options for Redis RPC client and server.
/// </summary>
public class RedisRpcOptions {
    /// <summary>
    /// Redis connection string (e.g., "localhost:6379").
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// Default timeout for RPC requests in milliseconds.
    /// </summary>
    public int DefaultTimeoutMs { get; set; } = 30000; // 30 seconds

    /// <summary>
    /// Maximum number of concurrent requests to handle.
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 100;

    /// <summary>
    /// Prefix for all Redis channels used by this RPC system.
    /// </summary>
    public string ChannelPrefix { get; set; } = "redis-rpc";

    /// <summary>
    /// Whether to include stack traces in error responses.
    /// Should be false in production environments.
    /// </summary>
    public bool IncludeStackTraceInErrors { get; set; } = false;

    /// <summary>
    /// Database index to use for Redis operations.
    /// </summary>
    public int Database { get; set; } = 0;
}
