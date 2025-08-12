using System.Text.Json.Serialization;

namespace RedisRpc.Core.Models;

/// <summary>
/// Represents an RPC request message that is sent via Redis Pub/Sub.
/// Contains the method name, parameters, and correlation ID for request/response matching.
/// </summary>
public class RpcRequest {
    /// <summary>
    /// Unique identifier for this request, used to correlate with the response.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The name of the method to invoke on the remote service.
    /// </summary>
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Parameters to pass to the remote method, serialized as JSON.
    /// </summary>
    [JsonPropertyName("parameters")]
    public object? Parameters { get; set; }

    /// <summary>
    /// Channel where the response should be sent back.
    /// </summary>
    [JsonPropertyName("responseChannel")]
    public string ResponseChannel { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the request was created (ISO 8601 format).
    /// </summary>
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = DateTime.UtcNow.ToString("O");

    /// <summary>
    /// Optional timeout in milliseconds for the request.
    /// </summary>
    [JsonPropertyName("timeoutMs")]
    public int? TimeoutMs { get; set; }
}
