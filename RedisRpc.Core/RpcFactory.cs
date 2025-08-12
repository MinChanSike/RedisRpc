using RedisRpc.Core.Implementation;
using RedisRpc.Core.Interfaces;
using RedisRpc.Core.Models;

namespace RedisRpc.Core;

/// <summary>
/// Factory class for creating RPC clients and servers with default configuration.
/// Provides a convenient way to set up Redis RPC components.
/// </summary>
public static class RpcFactory {
    /// <summary>
    /// Creates an RPC client with default options.
    /// </summary>
    /// <param name="connectionString">Redis connection string (default: "localhost:6379").</param>
    /// <returns>A new RPC client instance.</returns>
    public static IRpcClient CreateClient(string connectionString = "localhost:6379") {
        var options = new RedisRpcOptions {
            ConnectionString = connectionString
        };
        return new RpcClient(options);
    }

    /// <summary>
    /// Creates an RPC client with custom options.
    /// </summary>
    /// <param name="options">Custom Redis RPC options.</param>
    /// <returns>A new RPC client instance.</returns>
    public static IRpcClient CreateClient(RedisRpcOptions options) {
        return new RpcClient(options);
    }

    /// <summary>
    /// Creates an RPC server with default options.
    /// </summary>
    /// <param name="connectionString">Redis connection string (default: "localhost:6379").</param>
    /// <returns>A new RPC server instance.</returns>
    public static IRpcServer CreateServer(string connectionString = "localhost:6379") {
        var options = new RedisRpcOptions {
            ConnectionString = connectionString
        };
        return new RpcServer(options);
    }

    /// <summary>
    /// Creates an RPC server with custom options.
    /// </summary>
    /// <param name="options">Custom Redis RPC options.</param>
    /// <returns>A new RPC server instance.</returns>
    public static IRpcServer CreateServer(RedisRpcOptions options) {
        return new RpcServer(options);
    }

    /// <summary>
    /// Creates default Redis RPC options with sensible defaults for development.
    /// </summary>
    /// <param name="connectionString">Redis connection string.</param>
    /// <returns>Pre-configured options for development use.</returns>
    public static RedisRpcOptions CreateDefaultOptions(string connectionString = "localhost:6379") {
        return new RedisRpcOptions {
            ConnectionString = connectionString,
            DefaultTimeoutMs = 30000,
            MaxConcurrentRequests = 100,
            ChannelPrefix = "redis-rpc",
            IncludeStackTraceInErrors = true, // Good for development
            Database = 0
        };
    }

    /// <summary>
    /// Creates Redis RPC options optimized for production use.
    /// </summary>
    /// <param name="connectionString">Redis connection string.</param>
    /// <returns>Pre-configured options for production use.</returns>
    public static RedisRpcOptions CreateProductionOptions(string connectionString) {
        return new RedisRpcOptions {
            ConnectionString = connectionString,
            DefaultTimeoutMs = 15000, // Shorter timeout for production
            MaxConcurrentRequests = 200, // Higher concurrency for production
            ChannelPrefix = "redis-rpc",
            IncludeStackTraceInErrors = false, // Don't expose stack traces in production
            Database = 0
        };
    }
}
