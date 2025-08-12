using System.Text.Json.Serialization;

namespace RedisRpc.Core.Models;

/// <summary>
/// Represents an error that occurred during RPC method execution.
/// Provides structured error information with code, message, and optional details.
/// </summary>
public class RpcError
{
    /// <summary>
    /// Error code indicating the type of error.
    /// </summary>
    [JsonPropertyName("code")]
    public RpcErrorCode Code { get; set; }

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional additional details about the error.
    /// </summary>
    [JsonPropertyName("details")]
    public object? Details { get; set; }

    /// <summary>
    /// Stack trace if available (typically only included in development environments).
    /// </summary>
    [JsonPropertyName("stackTrace")]
    public string? StackTrace { get; set; }
}

/// <summary>
/// Standard RPC error codes.
/// </summary>
public enum RpcErrorCode
{
    /// <summary>
    /// Unknown or unspecified error.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The requested method was not found.
    /// </summary>
    MethodNotFound = 1001,

    /// <summary>
    /// Invalid parameters were provided.
    /// </summary>
    InvalidParameters = 1002,

    /// <summary>
    /// An internal server error occurred.
    /// </summary>
    InternalError = 1003,

    /// <summary>
    /// The request timed out.
    /// </summary>
    Timeout = 1004,

    /// <summary>
    /// Serialization or deserialization error.
    /// </summary>
    SerializationError = 1005,

    /// <summary>
    /// Connection or network error.
    /// </summary>
    ConnectionError = 1006
}
