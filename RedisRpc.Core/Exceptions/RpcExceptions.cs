using RedisRpc.Core.Models;

namespace RedisRpc.Core.Exceptions;

/// <summary>
/// Base exception for all Redis RPC related errors.
/// </summary>
public abstract class RpcException : Exception {
    /// <summary>
    /// The RPC error code associated with this exception.
    /// </summary>
    public RpcErrorCode ErrorCode { get; }

    /// <summary>
    /// Optional additional details about the error.
    /// </summary>
    public object? Details { get; }

    protected RpcException(RpcErrorCode errorCode, string message, object? details = null) : base(message) {
        ErrorCode = errorCode;
        Details = details;
    }

    protected RpcException(RpcErrorCode errorCode, string message, Exception innerException, object? details = null) : base(message, innerException) {
        ErrorCode = errorCode;
        Details = details;
    }

    /// <summary>
    /// Converts this exception to an RpcError object.
    /// </summary>
    public RpcError ToRpcError(bool includeStackTrace = false) {
        return new RpcError {
            Code = ErrorCode,
            Message = Message,
            Details = Details,
            StackTrace = includeStackTrace ? StackTrace : null
        };
    }
}

/// <summary>
/// Exception thrown when a requested RPC method is not found.
/// </summary>
public class MethodNotFoundException : RpcException {
    public MethodNotFoundException(string methodName) : base(RpcErrorCode.MethodNotFound, $"Method '{methodName}' not found", new { MethodName = methodName }) {
    }
}

/// <summary>
/// Exception thrown when invalid parameters are provided to an RPC method.
/// </summary>
public class InvalidParametersException : RpcException {
    public InvalidParametersException(string message, object? details = null) : base(RpcErrorCode.InvalidParameters, message, details) {
    }

    public InvalidParametersException(string message, Exception innerException, object? details = null)
        : base(RpcErrorCode.InvalidParameters, message, innerException, details) {
    }
}

/// <summary>
/// Exception thrown when an RPC request times out.
/// </summary>
public class RpcTimeoutException : RpcException {
    public RpcTimeoutException(int timeoutMs)
        : base(RpcErrorCode.Timeout, $"Request timed out after {timeoutMs}ms", new { TimeoutMs = timeoutMs }) {
    }
}

/// <summary>
/// Exception thrown when serialization/deserialization fails.
/// </summary>
public class SerializationException : RpcException {
    public SerializationException(string message, Exception innerException)
        : base(RpcErrorCode.SerializationError, message, innerException) {
    }
}

/// <summary>
/// Exception thrown when there are connection issues with Redis.
/// </summary>
public class ConnectionException : RpcException {
    public ConnectionException(string message, Exception innerException)
        : base(RpcErrorCode.ConnectionError, message, innerException) {
    }
}

/// <summary>
/// Exception thrown when an internal error occurs during RPC processing.
/// </summary>
public class InternalRpcException : RpcException {
    public InternalRpcException(string message, Exception innerException)
        : base(RpcErrorCode.InternalError, message, innerException) {
    }

    public InternalRpcException(string message)
        : base(RpcErrorCode.InternalError, message) {
    }
}
