using System.Text.Json.Serialization;

namespace RedisRpc.Core.Models;

/// <summary>
/// Represents an RPC response message that is sent back via Redis Pub/Sub.
/// Contains either a successful result or error information.
/// </summary>
public class RpcResponse {
    /// <summary>
    /// Correlation ID matching the original request.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the request was successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// The result data if the request was successful, serialized as JSON.
    /// </summary>
    [JsonPropertyName("result")]
    public object? Result { get; set; }

    /// <summary>
    /// Error information if the request failed.
    /// </summary>
    [JsonPropertyName("error")]
    public RpcError? Error { get; set; }

    /// <summary>
    /// Timestamp when the response was created (ISO 8601 format).
    /// </summary>
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = DateTime.UtcNow.ToString("O");
}
