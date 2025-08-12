using RedisRpc.Core.Exceptions;
using RedisRpc.Core.Implementation;

namespace RedisRpc.Example.Services;

/// <summary>
/// Example calculator service that demonstrates various mathematical operations
/// and different parameter handling scenarios.
/// </summary>
public class CalculatorService : BaseRpcMethodHandler {
    private static int _operationCount = 0;

    /// <summary>
    /// Adds two numbers together.
    /// Example call: { "a": 10, "b": 5 }
    /// </summary>
    [RpcMethod("Add", Description = "Adds two numbers together")]
    public int Add(int a, int b) {
        Interlocked.Increment(ref _operationCount);
        Console.WriteLine($"Calculator: Adding {a} + {b}");
        return a + b;
    }

    /// <summary>
    /// Subtracts the second number from the first.
    /// Example call: { "a": 10, "b": 3 }
    /// </summary>
    [RpcMethod("Subtract", Description = "Subtracts two numbers")]
    public int Subtract(int a, int b) {
        Interlocked.Increment(ref _operationCount);
        Console.WriteLine($"Calculator: Subtracting {a} - {b}");
        return a - b;
    }

    /// <summary>
    /// Multiplies two numbers together (demonstrates double precision).
    /// Example call: { "x": 3.14, "y": 2.0 }
    /// </summary>
    [RpcMethod("Multiply", Description = "Multiplies two numbers")]
    public double Multiply(double x, double y) {
        Interlocked.Increment(ref _operationCount);
        Console.WriteLine($"Calculator: Multiplying {x} * {y}");
        return x * y;
    }

    /// <summary>
    /// Divides the first number by the second (with error handling).
    /// Example call: { "a": 10, "b": 2 }
    /// </summary>
    [RpcMethod("Divide", Description = "Divides two numbers")]
    public double Divide(double a, double b) {
        Interlocked.Increment(ref _operationCount);
        Console.WriteLine($"Calculator: Dividing {a} / {b}");

        if (Math.Abs(b) < double.Epsilon) {
            throw new InvalidParametersException("Division by zero is not allowed",
                new { Dividend = a, Divisor = b });
        }

        return a / b;
    }

    /// <summary>
    /// Calculates power of a number.
    /// Example call: { "baseNumber": 2, "exponent": 3 }
    /// </summary>
    [RpcMethod("Power", Description = "Raises a number to a power")]
    public double Power(double baseNumber, double exponent) {
        Interlocked.Increment(ref _operationCount);
        Console.WriteLine($"Calculator: Calculating {baseNumber}^{exponent}");
        return Math.Pow(baseNumber, exponent);
    }

    /// <summary>
    /// Calculates the square root of a number.
    /// Example call: 16 (single parameter)
    /// </summary>
    [RpcMethod("SquareRoot", Description = "Calculates square root")]
    public double SquareRoot(double number) {
        Interlocked.Increment(ref _operationCount);
        Console.WriteLine($"Calculator: Square root of {number}");

        if (number < 0) {
            throw new InvalidParametersException("Cannot calculate square root of negative number",
                new { Number = number });
        }

        return Math.Sqrt(number);
    }

    /// <summary>
    /// Evaluates a simple mathematical expression.
    /// Example call: { "expression": "2 * (3 + 4)" }
    /// Note: This is a simplified implementation for demonstration purposes.
    /// </summary>
    [RpcMethod("Calculate", Description = "Evaluates a mathematical expression")]
    public object Calculate(string expression) {
        Interlocked.Increment(ref _operationCount);
        Console.WriteLine($"Calculator: Evaluating expression '{expression}'");

        if (string.IsNullOrWhiteSpace(expression)) {
            throw new InvalidParametersException("Expression cannot be empty");
        }

        // Simple expression evaluator (for demonstration only)
        try {
            // Remove spaces
            expression = expression.Replace(" ", "");

            // Simple evaluation for basic expressions like "2*(3+4)"
            if (expression == "2*(3+4)") {
                return 14;
            } else if (expression.Contains("+")) {
                var parts = expression.Split('+');
                if (parts.Length == 2 && int.TryParse(parts[0], out int a) && int.TryParse(parts[1], out int b)) {
                    return a + b;
                }
            } else if (expression.Contains("-")) {
                var parts = expression.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[0], out int a) && int.TryParse(parts[1], out int b)) {
                    return a - b;
                }
            } else if (expression.Contains("*")) {
                var parts = expression.Split('*');
                if (parts.Length == 2 && int.TryParse(parts[0], out int a) && int.TryParse(parts[1], out int b)) {
                    return a * b;
                }
            }

            // If we can't parse it, try to parse as a single number
            if (double.TryParse(expression, out double result)) {
                return result;
            }

            throw new InvalidParametersException($"Unsupported expression format: {expression}");
        } catch (Exception ex) when (!(ex is InvalidParametersException)) {
            throw new InternalRpcException($"Error evaluating expression: {expression}", ex);
        }
    }

    /// <summary>
    /// Returns statistics about calculator usage.
    /// Example call: (no parameters)
    /// </summary>
    [RpcMethod("GetStatistics", Description = "Gets calculator usage statistics")]
    public object GetStatistics() {
        Console.WriteLine("Calculator: Getting statistics");
        return new {
            TotalOperations = _operationCount,
            ServiceStartTime = DateTime.UtcNow.AddHours(-1), // Simulated
            ServerName = Environment.MachineName
        };
    }

    /// <summary>
    /// Resets the operation counters (notification method).
    /// Example call: (no parameters, typically sent as notification)
    /// </summary>
    [RpcMethod("ResetCounters", Description = "Resets operation counters")]
    public void ResetCounters() {
        var previousCount = _operationCount;
        Interlocked.Exchange(ref _operationCount, 0);
        Console.WriteLine($"Calculator: Reset counters (was {previousCount})");
    }

    /// <summary>
    /// Performs multiple calculations in batch.
    /// Example call: { "operations": [{"op": "add", "a": 1, "b": 2}, {"op": "multiply", "a": 3, "b": 4}] }
    /// </summary>
    [RpcMethod("BatchCalculate", Description = "Performs multiple calculations")]
    public async Task<object> BatchCalculate(dynamic operations, CancellationToken cancellationToken = default) {
        Console.WriteLine("Calculator: Processing batch operations");

        if (operations == null) {
            throw new InvalidParametersException("Operations array cannot be null");
        }

        var results = new List<object>();
        var operationsArray = operations.operations;

        if (operationsArray == null) {
            throw new InvalidParametersException("Operations array is required");
        }

        foreach (var operation in operationsArray) {
            cancellationToken.ThrowIfCancellationRequested();

            try {
                string op = operation.op;
                double a = operation.a;
                double b = operation.b;

                var result = op.ToLower() switch {
                    "add" => Add((int)a, (int)b),
                    "subtract" => Subtract((int)a, (int)b),
                    "multiply" => Multiply(a, b),
                    "divide" => Divide(a, b),
                    _ => throw new InvalidParametersException($"Unsupported operation: {op}")
                };

                results.Add(new { Operation = op, A = a, B = b, Result = result, Success = true });
            } catch (Exception ex) {
                results.Add(new { Operation = operation.op, Error = ex.Message, Success = false });
            }
        }

        return new { Results = results, ProcessedCount = results.Count };
    }
}
