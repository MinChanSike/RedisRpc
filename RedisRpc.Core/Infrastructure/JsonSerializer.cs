using RedisRpc.Core.Exceptions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RedisRpc.Core.Infrastructure;

/// <summary>
/// Provides JSON serialization and deserialization functionality for RPC messages.
/// Uses System.Text.Json with consistent settings across the library.
/// </summary>
public static class JsonSerializer {
    private static readonly JsonSerializerOptions Options = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    private static readonly JsonSerializerOptions PrettyPrintOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    /// <summary>
    /// Serializes an object to JSON string.
    /// </summary>
    /// <param name="obj">The object to serialize.</param>
    /// <returns>JSON string representation of the object.</returns>
    /// <exception cref="SerializationException">Thrown when serialization fails.</exception>
    public static string Serialize(object? obj) {
        try {
            return System.Text.Json.JsonSerializer.Serialize(obj, Options);
        } catch (Exception ex) {
            throw new SerializationException($"Failed to serialize object of type {obj?.GetType().Name ?? "null"}", ex);
        }
    }

    /// <summary>
    /// Deserializes a JSON string to the specified type.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize to.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>Deserialized object of type T.</returns>
    /// <exception cref="SerializationException">Thrown when deserialization fails.</exception>
    public static T? Deserialize<T>(string json) {
        try {
            if (string.IsNullOrWhiteSpace(json))
                return default(T);

            return System.Text.Json.JsonSerializer.Deserialize<T>(json, Options);
        } catch (Exception ex) {
            throw new SerializationException($"Failed to deserialize JSON to type {typeof(T).Name}", ex);
        }
    }

    /// <summary>
    /// Deserializes a JSON string to the specified type.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="type">The target type to deserialize to.</param>
    /// <returns>Deserialized object of the specified type.</returns>
    /// <exception cref="SerializationException">Thrown when deserialization fails.</exception>
    public static object? Deserialize(string json, Type type) {
        try {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            return System.Text.Json.JsonSerializer.Deserialize(json, type, Options);
        } catch (Exception ex) {
            throw new SerializationException($"Failed to deserialize JSON to type {type.Name}", ex);
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string, returning a boolean indicating success.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize to.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="result">The deserialized object if successful, default(T) otherwise.</param>
    /// <returns>True if deserialization succeeded, false otherwise.</returns>
    public static bool TryDeserialize<T>(string json, out T? result) {
        try {
            result = Deserialize<T>(json);
            return true;
        } catch {
            result = default(T);
            return false;
        }
    }

    /// <summary>
    /// Validates if a string is valid JSON.
    /// </summary>
    /// <param name="json">The JSON string to validate.</param>
    /// <returns>True if the string is valid JSON, false otherwise.</returns>
    public static bool IsValidJson(string json) {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try {
            using var document = JsonDocument.Parse(json);
            return true;
        } catch {
            return false;
        }
    }

    /// <summary>
    /// Pretty-prints a JSON string for debugging purposes.
    /// </summary>
    /// <param name="json">The JSON string to format.</param>
    /// <returns>Formatted JSON string.</returns>
    public static string PrettyPrint(string json) {
        try {
            using var document = JsonDocument.Parse(json);
            return System.Text.Json.JsonSerializer.Serialize(document, PrettyPrintOptions);
        } catch {
            return json; // Return original if formatting fails
        }
    }

    public static object? ConvertJsonElementToType(JsonElement jsonElement, Type targetType) {
        try {
            // Handle null values
            if (jsonElement.ValueKind == JsonValueKind.Null) {
                return GetDefaultValue(targetType);
            }

            // Handle primitive types directly
            if (targetType == typeof(string)) {
                return jsonElement.GetString();
            } else if (targetType == typeof(int) || targetType == typeof(int?)) {
                return jsonElement.GetInt32();
            } else if (targetType == typeof(long) || targetType == typeof(long?)) {
                return jsonElement.GetInt64();
            } else if (targetType == typeof(double) || targetType == typeof(double?)) {
                return jsonElement.GetDouble();
            } else if (targetType == typeof(decimal) || targetType == typeof(decimal?)) {
                return jsonElement.GetDecimal();
            } else if (targetType == typeof(bool) || targetType == typeof(bool?)) {
                return jsonElement.GetBoolean();
            } else if (targetType == typeof(DateTime) || targetType == typeof(DateTime?)) {
                return jsonElement.GetDateTime();
            }

            // For complex types, serialize the JsonElement to JSON string and then deserialize to target type
            var json = jsonElement.GetRawText();
            return Infrastructure.JsonSerializer.Deserialize(json, targetType);
        } catch (Exception ex) {
            throw new InvalidParametersException(
                $"Failed to convert JsonElement to type {targetType.Name}",
                ex,
                new { JsonElementKind = jsonElement.ValueKind, TargetType = targetType.Name });
        }
    }

    /// <summary>
    /// Converts a JsonElement to the specified target type.
    /// </summary>
    public static T ConvertJsonElementToTargetType<T>(JsonElement jsonElement) {
        var targetType = typeof(T);

        // Handle null values
        if (jsonElement.ValueKind == JsonValueKind.Null) {
            return default(T)!;
        }

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType);
        if (underlyingType != null) {
            targetType = underlyingType;
        }

        // Handle specific primitive types
        try {
            if (targetType == typeof(string)) {
                return (T)(object)jsonElement.GetString()!;
            } else if (targetType == typeof(int)) {
                return (T)(object)jsonElement.GetInt32();
            } else if (targetType == typeof(long)) {
                return (T)(object)jsonElement.GetInt64();
            } else if (targetType == typeof(double)) {
                return (T)(object)jsonElement.GetDouble();
            } else if (targetType == typeof(float)) {
                return (T)(object)(float)jsonElement.GetDouble();
            } else if (targetType == typeof(decimal)) {
                return (T)(object)jsonElement.GetDecimal();
            } else if (targetType == typeof(bool)) {
                return (T)(object)jsonElement.GetBoolean();
            } else if (targetType == typeof(DateTime)) {
                return (T)(object)jsonElement.GetDateTime();
            } else if (targetType == typeof(byte)) {
                return (T)(object)jsonElement.GetByte();
            } else if (targetType == typeof(short)) {
                return (T)(object)jsonElement.GetInt16();
            } else if (targetType == typeof(uint)) {
                return (T)(object)jsonElement.GetUInt32();
            } else if (targetType == typeof(ulong)) {
                return (T)(object)jsonElement.GetUInt64();
            } else if (targetType == typeof(ushort)) {
                return (T)(object)jsonElement.GetUInt16();
            } else if (targetType == typeof(sbyte)) {
                return (T)(object)jsonElement.GetSByte();
            } else if (targetType == typeof(Guid)) {
                return (T)(object)jsonElement.GetGuid();
            }

            // For complex types or other cases, deserialize from the raw JSON
            var json = jsonElement.GetRawText();
            return JsonSerializer.Deserialize<T>(json)!;
        } catch (Exception ex) {
            throw new SerializationException(
                $"Failed to convert JsonElement of kind {jsonElement.ValueKind} to type {typeof(T).Name}",
                ex);
        }
    }

    private static object? GetDefaultValue(Type type) {
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

}
