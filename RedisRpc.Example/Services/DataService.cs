using RedisRpc.Core.Exceptions;
using RedisRpc.Core.Implementation;
using System.Collections.Concurrent;

namespace RedisRpc.Example.Services;

/// <summary>
/// Example data service that demonstrates CRUD operations, data processing,
/// and more complex object handling patterns.
/// </summary>
public class DataService : BaseRpcMethodHandler {
    // In-memory storage for demonstration (in real scenarios, this would be a database)
    private static readonly ConcurrentDictionary<int, User> _users = new();
    private static readonly ConcurrentDictionary<string, object> _cache = new();
    private static readonly List<string> _activityLog = new();
    private static int _nextUserId = 1;

    /// <summary>
    /// Creates a new user in the system.
    /// Example call: { "name": "John Doe", "email": "john@example.com", "age": 30 }
    /// </summary>
    [RpcMethod("CreateUser", Description = "Creates a new user")]
    public User CreateUser(string name, string email, int age) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new InvalidParametersException("Name cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains("@")) {
            throw new InvalidParametersException("Valid email address is required");
        }

        if (age < 0 || age > 150) {
            throw new InvalidParametersException("Age must be between 0 and 150");
        }

        // Check if user with email already exists
        if (_users.Values.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase))) {
            throw new InvalidParametersException("User with this email already exists",
                new { Email = email });
        }

        var user = new User {
            Id = Interlocked.Increment(ref _nextUserId),
            Name = name.Trim(),
            Email = email.Trim().ToLower(),
            Age = age,
            CreatedAt = DateTime.UtcNow
        };

        _users[user.Id] = user;

        Console.WriteLine($"Data: Created user {user.Id} ({user.Name})");
        return user;
    }

    /// <summary>
    /// Gets a user by ID.
    /// Example call: 1 (single integer parameter)
    /// </summary>
    [RpcMethod("GetUser", Description = "Gets a user by ID")]
    public User? GetUser(int userId) {
        if (userId <= 0) {
            throw new InvalidParametersException("User ID must be positive");
        }

        _users.TryGetValue(userId, out var user);

        Console.WriteLine($"Data: Retrieved user {userId} (found: {user != null})");
        return user;
    }

    /// <summary>
    /// Gets all users in the system.
    /// Example call: (no parameters)
    /// </summary>
    [RpcMethod("GetAllUsers", Description = "Gets all users")]
    public List<User> GetAllUsers() {
        var users = _users.Values.OrderBy(u => u.Id).ToList();
        Console.WriteLine($"Data: Retrieved {users.Count} users");
        return users;
    }

    /// <summary>
    /// Updates an existing user.
    /// Example call: { "userId": 1, "name": "Jane Doe", "email": "jane@example.com", "age": 25 }
    /// </summary>
    [RpcMethod("UpdateUser", Description = "Updates an existing user")]
    public User? UpdateUser(int userId, string? name = null, string? email = null, int? age = null) {
        if (userId <= 0) {
            throw new InvalidParametersException("User ID must be positive");
        }

        if (!_users.TryGetValue(userId, out var user)) {
            throw new InvalidParametersException($"User with ID {userId} not found");
        }

        var updated = false;

        if (!string.IsNullOrWhiteSpace(name) && name.Trim() != user.Name) {
            user.Name = name.Trim();
            updated = true;
        }

        if (!string.IsNullOrWhiteSpace(email) && email.Trim().ToLower() != user.Email) {
            if (!email.Contains("@")) {
                throw new InvalidParametersException("Valid email address is required");
            }

            // Check for duplicate email
            if (_users.Values.Any(u => u.Id != userId && u.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase))) {
                throw new InvalidParametersException("User with this email already exists");
            }

            user.Email = email.Trim().ToLower();
            updated = true;
        }

        if (age.HasValue && age.Value != user.Age) {
            if (age.Value < 0 || age.Value > 150) {
                throw new InvalidParametersException("Age must be between 0 and 150");
            }

            user.Age = age.Value;
            updated = true;
        }

        if (updated) {
            user.UpdatedAt = DateTime.UtcNow;
            Console.WriteLine($"Data: Updated user {userId}");
        } else {
            Console.WriteLine($"Data: No changes for user {userId}");
        }

        return user;
    }

    /// <summary>
    /// Deletes a user by ID.
    /// Example call: 1 (single integer parameter)
    /// </summary>
    [RpcMethod("DeleteUser", Description = "Deletes a user by ID")]
    public bool DeleteUser(int userId) {
        if (userId <= 0) {
            throw new InvalidParametersException("User ID must be positive");
        }

        var removed = _users.TryRemove(userId, out var user);
        Console.WriteLine($"Data: Deleted user {userId} (success: {removed})");
        return removed;
    }

    /// <summary>
    /// Searches users by name or email.
    /// Example call: { "query": "john", "maxResults": 10 }
    /// </summary>
    [RpcMethod("SearchUsers", Description = "Searches users by name or email")]
    public List<User> SearchUsers(string query, int maxResults = 50) {
        if (string.IsNullOrWhiteSpace(query)) {
            return GetAllUsers().Take(maxResults).ToList();
        }

        if (maxResults <= 0) {
            maxResults = 50;
        }

        var searchTerm = query.Trim().ToLower();
        var results = _users.Values
            .Where(u => u.Name.ToLower().Contains(searchTerm) ||
                       u.Email.ToLower().Contains(searchTerm))
            .OrderBy(u => u.Name)
            .Take(maxResults)
            .ToList();

        Console.WriteLine($"Data: Found {results.Count} users matching '{query}'");
        return results;
    }

    /// <summary>
    /// Processes data array with various operations.
    /// Example call: { "data": [1, 2, 3, 4, 5], "operation": "sum" }
    /// </summary>
    [RpcMethod("ProcessData", Description = "Processes an array of data with specified operation")]
    public object ProcessData(int[] data, string operation) {
        if (data == null || data.Length == 0) {
            throw new InvalidParametersException("Data array cannot be null or empty");
        }

        if (string.IsNullOrWhiteSpace(operation)) {
            throw new InvalidParametersException("Operation cannot be empty");
        }

        var result = operation.ToLower() switch {
            "sum" => (object)new { Operation = operation, Result = data.Sum(), Count = data.Length },
            "average" => (object)new { Operation = operation, Result = data.Average(), Count = data.Length },
            "min" => (object)new { Operation = operation, Result = data.Min(), Count = data.Length },
            "max" => (object)new { Operation = operation, Result = data.Max(), Count = data.Length },
            "count" => (object)new { Operation = operation, Result = data.Length, Count = data.Length },
            "sort" => (object)new { Operation = operation, Result = data.OrderBy(x => x).ToArray(), Count = data.Length },
            "reverse" => (object)new { Operation = operation, Result = data.Reverse().ToArray(), Count = data.Length },
            "distinct" => (object)new { Operation = operation, Result = data.Distinct().ToArray(), Count = data.Distinct().Count() },
            _ => throw new InvalidParametersException($"Unsupported operation: {operation}",
                new { SupportedOperations = new[] { "sum", "average", "min", "max", "count", "sort", "reverse", "distinct" } })
        };

        Console.WriteLine($"Data: Processed {data.Length} items with operation '{operation}'");
        return result;
    }

    /// <summary>
    /// Logs an activity (typically called as notification).
    /// Example call: { "activity": "User login", "userId": 123, "timestamp": "2024-01-01T12:00:00Z" }
    /// </summary>
    [RpcMethod("LogActivity", Description = "Logs an activity")]
    public void LogActivity(string activity, int? userId = null, DateTime? timestamp = null) {
        if (string.IsNullOrWhiteSpace(activity)) {
            throw new InvalidParametersException("Activity description cannot be empty");
        }

        var logTimestamp = timestamp ?? DateTime.UtcNow;
        var logEntry = userId.HasValue
            ? $"[{logTimestamp:yyyy-MM-dd HH:mm:ss}] User {userId}: {activity}"
            : $"[{logTimestamp:yyyy-MM-dd HH:mm:ss}] {activity}";

        lock (_activityLog) {
            _activityLog.Add(logEntry);

            // Keep only the last 1000 entries
            if (_activityLog.Count > 1000) {
                _activityLog.RemoveRange(0, _activityLog.Count - 1000);
            }
        }

        Console.WriteLine($"Data: Logged activity - {logEntry}");
    }

    /// <summary>
    /// Gets recent activity log entries.
    /// Example call: { "count": 10 }
    /// </summary>
    [RpcMethod("GetActivityLog", Description = "Gets recent activity log entries")]
    public List<string> GetActivityLog(int count = 20) {
        if (count <= 0) {
            count = 20;
        }

        List<string> entries;
        lock (_activityLog) {
            entries = _activityLog.TakeLast(count).ToList();
        }

        Console.WriteLine($"Data: Retrieved {entries.Count} activity log entries");
        return entries;
    }

    /// <summary>
    /// Updates a cache entry (typically called as notification).
    /// Example call: { "key": "user_count", "value": 42 }
    /// </summary>
    [RpcMethod("UpdateCache", Description = "Updates a cache entry")]
    public void UpdateCache(string key, object value) {
        if (string.IsNullOrWhiteSpace(key)) {
            throw new InvalidParametersException("Cache key cannot be empty");
        }

        _cache[key] = value;
        Console.WriteLine($"Data: Updated cache entry '{key}' = {value}");
    }

    /// <summary>
    /// Gets a cache entry.
    /// Example call: "user_count" (single string parameter)
    /// </summary>
    [RpcMethod("GetCache", Description = "Gets a cache entry")]
    public object? GetCache(string key) {
        if (string.IsNullOrWhiteSpace(key)) {
            throw new InvalidParametersException("Cache key cannot be empty");
        }

        _cache.TryGetValue(key, out var value);
        Console.WriteLine($"Data: Retrieved cache entry '{key}' (found: {value != null})");
        return value;
    }

    /// <summary>
    /// Gets all cache entries.
    /// Example call: (no parameters)
    /// </summary>
    [RpcMethod("GetAllCache", Description = "Gets all cache entries")]
    public Dictionary<string, object> GetAllCache() {
        var cache = _cache.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        Console.WriteLine($"Data: Retrieved {cache.Count} cache entries");
        return cache;
    }

    /// <summary>
    /// Gets service statistics and information.
    /// Example call: (no parameters)
    /// </summary>
    [RpcMethod("GetServiceStats", Description = "Gets data service statistics")]
    public object GetServiceStats() {
        Console.WriteLine("Data: Returning service statistics");

        int activityLogCount;
        lock (_activityLog) {
            activityLogCount = _activityLog.Count;
        }

        return new {
            ServiceName = "DataService",
            Version = "1.0.0",
            Statistics = new {
                TotalUsers = _users.Count,
                CacheEntries = _cache.Count,
                ActivityLogEntries = activityLogCount,
                NextUserId = _nextUserId
            },
            ServerInfo = new {
                ServerTime = DateTime.UtcNow,
                ServerName = Environment.MachineName,
                ProcessId = Environment.ProcessId
            }
        };
    }
}

/// <summary>
/// User entity for demonstration purposes.
/// </summary>
public class User {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public override string ToString() {
        return $"User {Id}: {Name} ({Email}, {Age} years old)";
    }
}
