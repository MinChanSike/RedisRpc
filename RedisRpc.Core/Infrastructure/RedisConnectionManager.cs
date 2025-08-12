using StackExchange.Redis;
using RedisRpc.Core.Models;
using RedisRpc.Core.Exceptions;

namespace RedisRpc.Core.Infrastructure;

/// <summary>
/// Manages Redis connections and provides a thread-safe way to access Redis operations.
/// Implements singleton pattern to reuse connections across the application.
/// </summary>
public class RedisConnectionManager : IDisposable
{
    private readonly RedisRpcOptions _options;
    private readonly Lazy<ConnectionMultiplexer> _connectionLazy;
    private bool _disposed = false;

    /// <summary>
    /// Initializes a new instance of the RedisConnectionManager.
    /// </summary>
    /// <param name="options">Redis RPC configuration options.</param>
    public RedisConnectionManager(RedisRpcOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _connectionLazy = new Lazy<ConnectionMultiplexer>(CreateConnection);
    }

    /// <summary>
    /// Gets the Redis database instance.
    /// </summary>
    public IDatabase Database
    {
        get
        {
            ThrowIfDisposed();
            try
            {
                return _connectionLazy.Value.GetDatabase(_options.Database);
            }
            catch (Exception ex)
            {
                throw new ConnectionException("Failed to get Redis database", ex);
            }
        }
    }

    /// <summary>
    /// Gets the Redis subscriber for pub/sub operations.
    /// </summary>
    public ISubscriber Subscriber
    {
        get
        {
            ThrowIfDisposed();
            try
            {
                return _connectionLazy.Value.GetSubscriber();
            }
            catch (Exception ex)
            {
                throw new ConnectionException("Failed to get Redis subscriber", ex);
            }
        }
    }

    /// <summary>
    /// Gets the Redis server instance for administrative operations.
    /// </summary>
    public IServer Server
    {
        get
        {
            ThrowIfDisposed();
            try
            {
                var endpoints = _connectionLazy.Value.GetEndPoints();
                return _connectionLazy.Value.GetServer(endpoints.First());
            }
            catch (Exception ex)
            {
                throw new ConnectionException("Failed to get Redis server", ex);
            }
        }
    }

    /// <summary>
    /// Checks if the connection is currently established and healthy.
    /// </summary>
    public bool IsConnected
    {
        get
        {
            if (_disposed || !_connectionLazy.IsValueCreated)
                return false;

            try
            {
                return _connectionLazy.Value.IsConnected;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Tests the Redis connection by performing a ping operation.
    /// </summary>
    /// <returns>True if the connection is healthy, false otherwise.</returns>
    public async Task<bool> TestConnectionAsync()
    {
        if (_disposed)
            return false;

        try
        {
            var database = Database;
            await database.PingAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Creates and configures a new Redis connection.
    /// </summary>
    private ConnectionMultiplexer CreateConnection()
    {
        try
        {
            var config = ConfigurationOptions.Parse(_options.ConnectionString);
            
            // Configure connection options for better reliability
            config.AbortOnConnectFail = false;
            config.ConnectRetry = 3;
            config.ConnectTimeout = 5000;
            config.SyncTimeout = 5000;
            config.AsyncTimeout = 5000;
            config.KeepAlive = 180;

            var connection = ConnectionMultiplexer.Connect(config);
            
            // Set up connection event handlers
            connection.ConnectionFailed += OnConnectionFailed;
            connection.ConnectionRestored += OnConnectionRestored;
            connection.ErrorMessage += OnErrorMessage;

            return connection;
        }
        catch (Exception ex)
        {
            throw new ConnectionException($"Failed to connect to Redis at '{_options.ConnectionString}'", ex);
        }
    }

    /// <summary>
    /// Handles Redis connection failure events.
    /// </summary>
    private void OnConnectionFailed(object? sender, ConnectionFailedEventArgs e)
    {
        // Log connection failure (in a real application, you'd use a proper logger)
        Console.WriteLine($"Redis connection failed: {e.Exception?.Message} - {e.FailureType}");
    }

    /// <summary>
    /// Handles Redis connection restoration events.
    /// </summary>
    private void OnConnectionRestored(object? sender, ConnectionFailedEventArgs e)
    {
        // Log connection restoration (in a real application, you'd use a proper logger)
        Console.WriteLine($"Redis connection restored: {e.ConnectionType}");
    }

    /// <summary>
    /// Handles Redis error message events.
    /// </summary>
    private void OnErrorMessage(object? sender, RedisErrorEventArgs e)
    {
        // Log Redis errors (in a real application, you'd use a proper logger)
        Console.WriteLine($"Redis error: {e.Message}");
    }

    /// <summary>
    /// Throws ObjectDisposedException if the manager has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RedisConnectionManager));
    }

    /// <summary>
    /// Disposes the Redis connection manager and all associated resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        if (_connectionLazy.IsValueCreated)
        {
            _connectionLazy.Value.Close();
            _connectionLazy.Value.Dispose();
        }

        _disposed = true;
    }
}
