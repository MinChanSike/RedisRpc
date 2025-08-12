using RedisRpc.Core.Exceptions;
using RedisRpc.Core.Infrastructure;
using RedisRpc.Core.Interfaces;
using System.Reflection;

namespace RedisRpc.Core.Implementation;

/// <summary>
/// Base class for RPC method handlers that provides reflection-based method dispatch.
/// Inherit from this class and add methods with RpcMethodAttribute to easily create handlers.
/// </summary>
public abstract class BaseRpcMethodHandler : IRpcMethodHandler {
    private readonly Dictionary<string, MethodInfo> _methods;

    /// <summary>
    /// Initializes the base method handler and discovers RPC methods via reflection.
    /// </summary>
    protected BaseRpcMethodHandler() {
        _methods = DiscoverRpcMethods();
    }

    /// <summary>
    /// Gets the collection of method names that this handler supports.
    /// </summary>
    public virtual IEnumerable<string> SupportedMethods => _methods.Keys;

    /// <summary>
    /// Handles an RPC method invocation using reflection to call the appropriate method.
    /// </summary>
    public virtual async Task<object?> HandleMethodAsync(string method, object? parameters, CancellationToken cancellationToken = default) {
        if (!_methods.TryGetValue(method, out var methodInfo)) {
            throw new MethodNotFoundException(method);
        }

        try {
            // Prepare method parameters
            var methodParameters = PrepareMethodParameters(methodInfo, parameters, cancellationToken);

            // Invoke the method
            var result = methodInfo.Invoke(this, methodParameters);

            // Handle async methods
            if (result is Task task) {
                await task;

                // If it's Task<T>, get the result
                if (task.GetType().IsGenericType) {
                    var property = task.GetType().GetProperty("Result");
                    return property?.GetValue(task);
                }

                return null; // Task (void)
            }

            return result;
        } catch (TargetInvocationException ex) {
            // Unwrap the actual exception
            throw ex.InnerException ?? ex;
        } catch (Exception ex) {
            throw new InternalRpcException($"Error invoking method '{method}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Discovers RPC methods in the derived class using reflection.
    /// Methods must be marked with RpcMethodAttribute or follow naming convention.
    /// </summary>
    private Dictionary<string, MethodInfo> DiscoverRpcMethods() {
        var methods = new Dictionary<string, MethodInfo>(StringComparer.OrdinalIgnoreCase);
        var type = GetType();

        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance)) {
            // Skip inherited methods from object and base classes
            if (method.DeclaringType == typeof(object) ||
                method.DeclaringType == typeof(BaseRpcMethodHandler))
                continue;

            // Check for RpcMethodAttribute
            var attribute = method.GetCustomAttribute<RpcMethodAttribute>();
            if (attribute != null) {
                var methodName = string.IsNullOrWhiteSpace(attribute.MethodName)
                    ? method.Name
                    : attribute.MethodName;
                methods[methodName] = method;
                continue;
            }

            // Auto-discover public methods that don't start with "Handle" or special names
            if (!method.Name.StartsWith("Handle", StringComparison.OrdinalIgnoreCase) &&
                !method.Name.Equals("ToString", StringComparison.OrdinalIgnoreCase) &&
                !method.Name.Equals("GetHashCode", StringComparison.OrdinalIgnoreCase) &&
                !method.Name.Equals("Equals", StringComparison.OrdinalIgnoreCase) &&
                !method.Name.Equals("GetType", StringComparison.OrdinalIgnoreCase) &&
                !method.Name.StartsWith("get_", StringComparison.OrdinalIgnoreCase) &&
                !method.Name.StartsWith("set_", StringComparison.OrdinalIgnoreCase)) {
                methods[method.Name] = method;
            }
        }

        return methods;
    }

    /// <summary>
    /// Prepares method parameters for invocation, handling deserialization and type conversion.
    /// </summary>
    private object?[] PrepareMethodParameters(MethodInfo method, object? parameters, CancellationToken cancellationToken) {
        var parameterInfos = method.GetParameters();
        var parameterValues = new object?[parameterInfos.Length];

        for (int i = 0; i < parameterInfos.Length; i++) {
            var paramInfo = parameterInfos[i];

            // Handle CancellationToken parameter
            if (paramInfo.ParameterType == typeof(CancellationToken)) {
                parameterValues[i] = cancellationToken;
                continue;
            }

            // Handle single parameter methods
            var isJsonObjElement = IsJsonObjElement(parameters);
            if (!isJsonObjElement && (parameterInfos.Length == 1 || (parameterInfos.Length == 2 && parameterInfos.Any(p => p.ParameterType == typeof(CancellationToken))))) {
                parameterValues[i] = ConvertParameter(parameters, paramInfo.ParameterType);
                continue;
            }

            // Handle multiple parameters - expect object with properties matching parameter names
            if (parameters != null) {
                var value = GetParameterValue(parameters, paramInfo.Name!, paramInfo.ParameterType);
                parameterValues[i] = value;
            } else if (paramInfo.HasDefaultValue) {
                parameterValues[i] = paramInfo.DefaultValue;
            } else {
                parameterValues[i] = GetDefaultValue(paramInfo.ParameterType);
            }
        }
        return parameterValues;
    }

    private static bool IsJsonObjElement(object? parameters) {
        if (parameters is System.Text.Json.JsonElement jsonElement && jsonElement.ValueKind == System.Text.Json.JsonValueKind.Object) {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets a parameter value from a complex object by property name.
    /// </summary>
    private object? GetParameterValue(object parameters, string parameterName, Type parameterType) {
        try {
            if (parameters is System.Text.Json.JsonElement jsonElement) {
                // Convert JsonElement to the target parameter type
                if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Object && jsonElement.TryGetProperty(parameterName, out var jsonProperty)) {
                    return JsonSerializer.ConvertJsonElementToType(jsonProperty, parameterType);
                }
                return GetDefaultValue(parameterType);
            }

            // Try to get property value using reflection
            var property = parameters.GetType().GetProperty(parameterName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (property != null) {
                var value = property.GetValue(parameters);
                return ConvertParameter(value, parameterType);
            }

            // Try as dictionary
            if (parameters is IDictionary<string, object> dict) {
                if (dict.TryGetValue(parameterName, out var value)) {
                    return ConvertParameter(value, parameterType);
                }
            }

            return GetDefaultValue(parameterType);
        } catch (Exception ex) {
            throw new InvalidParametersException(
                $"Failed to extract parameter '{parameterName}' of type {parameterType.Name}",
                ex,
                new { ParameterName = parameterName, ParameterType = parameterType.Name });
        }
    }

    /// <summary>
    /// Converts a parameter value to the target type.
    /// </summary>
    private object? ConvertParameter(object? value, Type targetType) {
        if (value == null)
            return GetDefaultValue(targetType);

        if (targetType.IsAssignableFrom(value.GetType()))
            return value;

        try {
            // Handle JsonElement values (from System.Text.Json deserialization)
            if (value is System.Text.Json.JsonElement jsonElement) {
                return JsonSerializer.ConvertJsonElementToType(jsonElement, targetType);
            }

            // Handle primitive type conversion
            if (targetType.IsPrimitive || targetType == typeof(string) || targetType == typeof(decimal)) {
                return Convert.ChangeType(value, targetType);
            }

            // Handle complex type deserialization
            var json = JsonSerializer.Serialize(value);
            return JsonSerializer.Deserialize(json, targetType);
        } catch (Exception ex) {
            throw new InvalidParametersException(
                $"Failed to convert parameter to type {targetType.Name}",
                ex,
                new { Value = value, TargetType = targetType.Name });
        }
    }

    /// <summary>
    /// Converts a JsonElement to the specified target type.
    /// </summary>


    /// <summary>
    /// Gets the default value for a type.
    /// </summary>
    private static object? GetDefaultValue(Type type) {
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}

/// <summary>
/// Attribute to mark methods as RPC endpoints and optionally specify a custom method name.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class RpcMethodAttribute : Attribute {
    /// <summary>
    /// Optional custom name for the RPC method. If not specified, uses the actual method name.
    /// </summary>
    public string? MethodName { get; set; }

    /// <summary>
    /// Optional description of what the method does.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Initializes a new instance of RpcMethodAttribute.
    /// </summary>
    /// <param name="methodName">Optional custom name for the RPC method.</param>
    public RpcMethodAttribute(string? methodName = null) {
        MethodName = methodName;
    }
}
