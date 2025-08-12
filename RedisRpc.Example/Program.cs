using RedisRpc.Core;
using RedisRpc.Core.Interfaces;
using RedisRpc.Example.Services;
using System.Text;

namespace RedisRpc.Example;

/// <summary>
/// Demonstration of Redis RPC functionality with both client and server examples.
/// This example shows how to:
/// 1. Set up an RPC server with method handlers
/// 2. Create an RPC client to make requests
/// 3. Handle various types of requests (typed, untyped, notifications)
/// 4. Demonstrate error handling scenarios
/// </summary>
public class Program {
    public static async Task Main(string[] args) {
        // Configure console for Unicode output
        Console.OutputEncoding = Encoding.UTF8;

        Console.WriteLine("=== Redis RPC Example ===");
        Console.WriteLine("This example demonstrates Redis-based RPC communication.");
        Console.WriteLine("Make sure Redis is running on localhost:6379 before continuing.");
        Console.WriteLine();

        // Check command line arguments
        if (args.Length > 0) {
            switch (args[0].ToLower()) {
                case "server":
                    await RunServerOnly();
                    return;
                case "client":
                    await RunClientOnly();
                    return;
                case "demo":
                    await RunFullDemo();
                    return;
            }
        }

        // Run full demonstration by default
        await RunFullDemo();
    }

    /// <summary>
    /// Runs the complete demonstration showing server setup and client interactions.
    /// </summary>
    private static async Task RunFullDemo() {
        Console.WriteLine("Starting full RPC demonstration...");

        using var cancellationTokenSource = new CancellationTokenSource();

        // Handle Ctrl+C gracefully
        Console.CancelKeyPress += (_, e) => {
            e.Cancel = true;
            cancellationTokenSource.Cancel();
            Console.WriteLine("\nShutdown requested...");
        };

        try {
            // Start the server in a background task
            var serverTask = Task.Run(() => RunServer(cancellationTokenSource.Token));

            // Wait a moment for server to start
            await Task.Delay(2000);

            // Run client demonstrations
            await RunClient();

            // Cancel the server
            cancellationTokenSource.Cancel();

            // Wait for server to shut down
            await serverTask;
        } catch (Exception ex) {
            Console.WriteLine($"Error during demonstration: {ex.Message}");
        }

        Console.WriteLine("Demonstration completed. Press any key to exit.");
        Console.ReadKey();
    }

    /// <summary>
    /// Runs only the server component (useful for testing with external clients).
    /// </summary>
    private static async Task RunServerOnly() {
        Console.WriteLine("Starting RPC server only...");
        Console.WriteLine("Press Ctrl+C to stop the server.");

        using var cancellationTokenSource = new CancellationTokenSource();

        Console.CancelKeyPress += (_, e) => {
            e.Cancel = true;
            cancellationTokenSource.Cancel();
        };

        await RunServer(cancellationTokenSource.Token);
        Console.WriteLine("Server stopped.");
    }

    /// <summary>
    /// Runs only the client component (assumes server is running elsewhere).
    /// </summary>
    private static async Task RunClientOnly() {
        Console.WriteLine("Running RPC client only...");
        Console.WriteLine("Make sure an RPC server is running on the same Redis instance.");
        Console.WriteLine();

        await RunClient();

        Console.WriteLine("Client demonstration completed. Press any key to exit.");
        Console.ReadKey();
    }

    /// <summary>
    /// Sets up and runs the RPC server with example services.
    /// </summary>
    private static async Task RunServer(CancellationToken cancellationToken) {
        try {
            // Create server with default options
            using var server = RpcFactory.CreateServer();

            // Register service handlers
            server.RegisterHandler(new CalculatorService());
            server.RegisterHandler(new GreetingService());
            server.RegisterHandler(new DataService());

            Console.WriteLine("🚀 RPC Server started successfully!");
            Console.WriteLine("📡 Listening on channels: calculator, greeting, data");
            Console.WriteLine("⏱️  Default timeout: 30 seconds");
            Console.WriteLine();

            // Start listening on multiple channels
            var channels = new[] { "calculator", "greeting", "data" };
            await server.StartListeningAsync(channels, cancellationToken);

            // Keep the server running until cancellation is requested
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(() => tcs.SetResult(true));
            await tcs.Task;

            Console.WriteLine("🛑 Stopping RPC server...");
            await server.StopListeningAsync();
            Console.WriteLine("✅ RPC server stopped.");
        } catch (Exception ex) {
            Console.WriteLine($"❌ Server error: {ex.Message}");
            Console.WriteLine($"💡 Make sure Redis is running and accessible.");
        }
    }

    /// <summary>
    /// Demonstrates various RPC client operations.
    /// </summary>
    private static async Task RunClient() {
        try {
            // Create client with default options
            using var client = RpcFactory.CreateClient();

            Console.WriteLine("🔧 RPC Client created successfully!");
            Console.WriteLine();

            // Demonstrate calculator service calls
            await DemonstrateCalculatorService(client);
            await Task.Delay(500);

            // Demonstrate greeting service calls
            await DemonstrateGreetingService(client);
            await Task.Delay(500);

            // Demonstrate data service calls
            await DemonstrateDataService(client);
            await Task.Delay(500);

            // Demonstrate notifications (fire-and-forget)
            await DemonstrateNotifications(client);

            // Demonstrate error handling
            await DemonstrateErrorHandling(client);
            await Task.Delay(500);

            Console.WriteLine("✅ All client demonstrations completed!");
        } catch (Exception ex) {
            Console.WriteLine($"❌ Client error: {ex.Message}");
        }
    }

    /// <summary>
    /// Demonstrates calculator service RPC calls.
    /// </summary>
    private static async Task DemonstrateCalculatorService(IRpcClient client) {
        Console.WriteLine("🧮 === Calculator Service Demo ===");

        try {
            // Simple addition
            var addResult = await client.SendRequestAsync<int>("calculator", "Add", new { a = 10, b = 5 });
            Console.WriteLine($"10 + 5 = {addResult}");

            // Multiplication with different parameter style
            var multiplyResult = await client.SendRequestAsync<double>("calculator", "Multiply", new { x = 3.14, y = 2.0 });
            Console.WriteLine($"3.14 * 2.0 = {multiplyResult}");

            // Complex calculation
            var complexResult = await client.SendRequestAsync<dynamic>("calculator", "Calculate", new { expression = "2 * (3 + 4)" });
            Console.WriteLine($"2 * (3 + 4) = {complexResult}");
        } catch (Exception ex) {
            Console.WriteLine($"Calculator error: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates greeting service RPC calls.
    /// </summary>
    private static async Task DemonstrateGreetingService(IRpcClient client) {
        Console.WriteLine("👋 === Greeting Service Demo ===");

        try {
            // Simple greeting
            var greeting1 = await client.SendRequestAsync<string>("greeting", "SayHello", "Alice");
            Console.WriteLine($"Greeting: {greeting1}");

            // Personalized greeting with multiple parameters
            var greeting2 = await client.SendRequestAsync<string>("greeting", "PersonalizedGreeting",
                new { name = "Bob", timeOfDay = "morning" });
            Console.WriteLine($"Personalized: {greeting2}");

            // Get current time from server
            var serverTime = await client.SendRequestAsync<DateTime>("greeting", "GetCurrentTime");
            Console.WriteLine($"Server time: {serverTime:yyyy-MM-dd HH:mm:ss} UTC");
        } catch (Exception ex) {
            Console.WriteLine($"Greeting error: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates data service RPC calls with complex objects.
    /// </summary>
    private static async Task DemonstrateDataService(IRpcClient client) {
        Console.WriteLine("📊 === Data Service Demo ===");

        try {
            // Create a user
            var newUser = new {
                name = "John Doe",
                email = "john.doe@example.com",
                age = 30
            };

            var createdUser = await client.SendRequestAsync<dynamic>("data", "CreateUser", newUser);
            Console.WriteLine($"Created user: {createdUser}");
                         
            // Get user by ID (assuming the created user has an ID)
            if (createdUser is not null) {
                // Method 1: Cast the dynamic to JsonElement and extract the property
                int userId;
                if (createdUser is System.Text.Json.JsonElement jsonElement) {
                    userId = jsonElement.GetProperty("id").GetInt32();
                } else {
                    // Fallback if it's actually a dynamic object
                    userId = (int)createdUser.id;
                }

                var retrievedUser = await client.SendRequestAsync<dynamic>("data", "GetUser", userId);
                Console.WriteLine($"Retrieved user: {retrievedUser}");
            }

            // Get all users
            var allUsers = await client.SendRequestAsync<List<dynamic>>("data", "GetAllUsers");
            Console.WriteLine($"Total users: {allUsers?.Count ?? 0}");

            // Process data with complex parameters
            var processResult = await client.SendRequestAsync<dynamic>("data", "ProcessData",
                new { data = new[] { 1, 2, 3, 4, 5 }, operation = "sum" });
            Console.WriteLine($"Data processing result: {processResult}");
        } catch (Exception ex) {
            Console.WriteLine($"Data service error: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates error handling scenarios.
    /// </summary>
    private static async Task DemonstrateErrorHandling(IRpcClient client) {
        Console.WriteLine("⚠️  === Error Handling Demo ===");

        // Test method not found
        try {
            await client.SendRequestAsync("calculator", "NonExistentMethod");
            Console.WriteLine("This shouldn't print - method should not exist!");
        } catch (Exception ex) {
            Console.WriteLine($"✅ Method not found handled: {ex.GetType().Name} - {ex.Message}");
        }

        // Test invalid parameters
        try {
            await client.SendRequestAsync("calculator", "Add", "invalid parameters");
        } catch (Exception ex) {
            Console.WriteLine($"✅ Invalid parameters handled: {ex.GetType().Name} - {ex.Message}");
        }

        // Test division by zero
        try {
            await client.SendRequestAsync<double>("calculator", "Divide", new { a = 10, b = 0 });
        } catch (Exception ex) {
            Console.WriteLine($"✅ Division by zero handled: {ex.GetType().Name} - {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates fire-and-forget notifications.
    /// </summary>
    private static async Task DemonstrateNotifications(IRpcClient client) {
        Console.WriteLine("🔥 === Notifications (Fire-and-Forget) Demo ===");

        try {
            // Send notification to log something (no response expected)
            await client.SendNotificationAsync("data", "LogActivity",
                new { activity = "User login", userId = 123, timestamp = DateTime.UtcNow });
            Console.WriteLine("✅ Notification sent: LogActivity");

            // Send notification to update cache
            await client.SendNotificationAsync("data", "UpdateCache",
                new { key = "user_count", value = 42 });
            Console.WriteLine("✅ Notification sent: UpdateCache");

            // Send notification to calculator service
            await client.SendNotificationAsync("calculator", "ResetCounters");
            Console.WriteLine("✅ Notification sent: ResetCounters");

            Console.WriteLine("📤 All notifications sent (no responses expected)");
        } catch (Exception ex) {
            Console.WriteLine($"Notification error: {ex.Message}");
        }

        Console.WriteLine();
    }
}
