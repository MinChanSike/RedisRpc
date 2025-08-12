# API Documentation

This document provides detailed information about the Redis RPC API, including all available methods, parameters, and response formats.

## Core Interfaces

### IRpcClient

The client interface for sending RPC requests.

#### Methods

##### SendRequestAsync<T>(channel, method, parameters, timeoutMs, cancellationToken)

Sends a typed RPC request and waits for a response.

**Parameters:**

- `channel` (string): Redis channel to send the request to
- `method` (string): Method name to invoke
- `parameters` (object): Parameters for the method (optional)
- `timeoutMs` (int?): Optional timeout in milliseconds
- `cancellationToken` (CancellationToken): Cancellation token (optional)

**Returns:** `Task<T?>` - Deserialized response of type T

**Example:**

```csharp
var result = await client.SendRequestAsync<int>("calculator", "Add", new { a = 5, b = 3 });
```

##### SendRequestAsync(channel, method, parameters, timeoutMs, cancellationToken)

Sends an untyped RPC request and waits for a response.

**Parameters:** Same as above

**Returns:** `Task<object?>` - Raw response object

**Example:**

```csharp
var result = await client.SendRequestAsync("greeting", "SayHello", "Alice");
```

##### SendNotificationAsync(channel, method, parameters, cancellationToken)

Sends a fire-and-forget notification (no response expected).

**Parameters:**

- `channel` (string): Redis channel to send the notification to
- `method` (string): Method name to invoke
- `parameters` (object): Parameters for the method (optional)
- `cancellationToken` (CancellationToken): Cancellation token (optional)

**Returns:** `Task` - Completes when the notification is sent

**Example:**

```csharp
await client.SendNotificationAsync("data", "LogActivity",
    new { activity = "User login", userId = 123 });
```

### IRpcServer

The server interface for handling RPC requests.

#### Methods

##### RegisterHandler(handler)

Registers a method handler for processing RPC requests.

**Parameters:**

- `handler` (IRpcMethodHandler): The handler to register

**Example:**

```csharp
server.RegisterHandler(new CalculatorService());
```

##### StartListeningAsync(channel, cancellationToken)

Starts listening for RPC requests on a single channel.

**Parameters:**

- `channel` (string): Redis channel to listen on
- `cancellationToken` (CancellationToken): Cancellation token to stop listening

##### StartListeningAsync(channels, cancellationToken)

Starts listening for RPC requests on multiple channels.

**Parameters:**

- `channels` (IEnumerable<string>): Redis channels to listen on
- `cancellationToken` (CancellationToken): Cancellation token to stop listening

**Example:**

```csharp
await server.StartListeningAsync(new[] { "calculator", "greeting", "data" }, cancellationToken);
```

##### StopListeningAsync()

Stops listening on all channels.

**Returns:** `Task` - Completes when all channels are unsubscribed

### IRpcMethodHandler

Interface for implementing RPC method handlers.

#### Properties

##### SupportedMethods

Gets the collection of method names that this handler can process.

**Returns:** `IEnumerable<string>`

#### Methods

##### HandleMethodAsync(method, parameters, cancellationToken)

Handles an RPC method invocation asynchronously.

**Parameters:**

- `method` (string): The name of the method to invoke
- `parameters` (object?): The parameters for the method invocation
- `cancellationToken` (CancellationToken): Cancellation token for the operation

**Returns:** `Task<object?>` - The result of the method invocation

## Message Formats

### RpcRequest

Represents an RPC request message.

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "method": "Add",
  "parameters": {
    "a": 10,
    "b": 5
  },
  "responseChannel": "redis-rpc:response:machine:1234:abc123",
  "timestamp": "2024-01-01T12:00:00.000Z",
  "timeoutMs": 30000
}
```

**Fields:**

- `id` (string): Unique identifier for request/response correlation
- `method` (string): Method name to invoke
- `parameters` (object): Method parameters (can be null)
- `responseChannel` (string): Channel for response (empty for notifications)
- `timestamp` (string): ISO 8601 timestamp when request was created
- `timeoutMs` (int?): Optional timeout in milliseconds

### RpcResponse

Represents an RPC response message.

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "success": true,
  "result": 15,
  "timestamp": "2024-01-01T12:00:01.000Z"
}
```

**Success Response Fields:**

- `id` (string): Correlation ID matching the request
- `success` (boolean): Always true for successful responses
- `result` (object): The method result (can be null)
- `timestamp` (string): ISO 8601 timestamp when response was created

**Error Response:**

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "success": false,
  "error": {
    "code": 1002,
    "message": "Invalid parameters",
    "details": {
      "parameterName": "b",
      "expectedType": "number"
    },
    "stackTrace": "..."
  },
  "timestamp": "2024-01-01T12:00:01.000Z"
}
```

**Error Response Fields:**

- `id` (string): Correlation ID matching the request
- `success` (boolean): Always false for error responses
- `error` (RpcError): Error information
- `timestamp` (string): ISO 8601 timestamp when response was created

### RpcError

Represents error information in RPC responses.

**Fields:**

- `code` (RpcErrorCode): Error code indicating the type of error
- `message` (string): Human-readable error message
- `details` (object): Optional additional error details
- `stackTrace` (string): Optional stack trace (included based on configuration)

### RpcErrorCode Enumeration

| Code | Name               | Description                            |
| ---- | ------------------ | -------------------------------------- |
| 0    | Unknown            | Unknown or unspecified error           |
| 1001 | MethodNotFound     | The requested method was not found     |
| 1002 | InvalidParameters  | Invalid parameters were provided       |
| 1003 | InternalError      | An internal server error occurred      |
| 1004 | Timeout            | The request timed out                  |
| 1005 | SerializationError | Serialization or deserialization error |
| 1006 | ConnectionError    | Connection or network error            |

## Redis Channel Structure

The library uses a structured channel naming convention:

- **Request channels**: `{channelPrefix}:request:{serviceName}`
- **Response channels**: `{channelPrefix}:response:{machineName}:{processId}:{guid}`

**Default channel prefix**: `redis-rpc`

**Example channels:**

- Request: `redis-rpc:request:calculator`
- Response: `redis-rpc:response:SERVER01:1234:abc123def456`

## Configuration

### RedisRpcOptions

Configuration options for Redis RPC components.

```csharp
public class RedisRpcOptions
{
    public string ConnectionString { get; set; } = "localhost:6379";
    public int DefaultTimeoutMs { get; set; } = 30000;
    public int MaxConcurrentRequests { get; set; } = 100;
    public string ChannelPrefix { get; set; } = "redis-rpc";
    public bool IncludeStackTraceInErrors { get; set; } = false;
    public int Database { get; set; } = 0;
}
```

## Factory Methods

### RpcFactory

Provides convenient factory methods for creating RPC components.

#### CreateClient Methods

```csharp
// Default options
IRpcClient CreateClient(string connectionString = "localhost:6379")

// Custom options
IRpcClient CreateClient(RedisRpcOptions options)
```

#### CreateServer Methods

```csharp
// Default options
IRpcServer CreateServer(string connectionString = "localhost:6379")

// Custom options
IRpcServer CreateServer(RedisRpcOptions options)
```

#### Configuration Methods

```csharp
// Default development options
RedisRpcOptions CreateDefaultOptions(string connectionString = "localhost:6379")

// Production-optimized options
RedisRpcOptions CreateProductionOptions(string connectionString)
```

## Exception Handling

### RpcException Hierarchy

All RPC-specific exceptions inherit from `RpcException`:

- `MethodNotFoundException`: Method not found
- `InvalidParametersException`: Invalid parameters
- `RpcTimeoutException`: Request timeout
- `SerializationException`: JSON serialization error
- `ConnectionException`: Redis connection error
- `InternalRpcException`: Internal processing error

### Exception to Error Code Mapping

```csharp
MethodNotFoundException     → RpcErrorCode.MethodNotFound
InvalidParametersException  → RpcErrorCode.InvalidParameters
RpcTimeoutException         → RpcErrorCode.Timeout
SerializationException      → RpcErrorCode.SerializationError
ConnectionException         → RpcErrorCode.ConnectionError
InternalRpcException        → RpcErrorCode.InternalError
Other exceptions            → RpcErrorCode.InternalError
```

## Best Practices

### Method Handler Implementation

1. **Inherit from BaseRpcMethodHandler** for automatic method discovery
2. **Use RpcMethodAttribute** to customize method names and descriptions
3. **Validate parameters** and throw appropriate exceptions
4. **Handle cancellation tokens** for long-running operations
5. **Return consistent result types** for predictable client behavior

### Error Handling

1. **Use specific exception types** for different error conditions
2. **Provide meaningful error messages** and details
3. **Don't expose sensitive information** in error responses
4. **Log errors appropriately** for debugging and monitoring

### Performance

1. **Reuse client and server instances** - they are thread-safe
2. **Configure appropriate timeouts** based on expected processing times
3. **Monitor Redis memory usage** and connection counts
4. **Consider connection pooling** for high-throughput scenarios

### Security

1. **Validate all input parameters** thoroughly
2. **Use Redis authentication** in production environments
3. **Consider SSL/TLS** for Redis connections
4. **Implement rate limiting** if needed
5. **Sanitize error messages** to prevent information disclosure
