using RedisRpc.Core.Exceptions;
using RedisRpc.Core.Implementation;

namespace RedisRpc.Example.Services;

/// <summary>
/// Example greeting service that demonstrates string manipulation,
/// date/time operations, and different parameter handling patterns.
/// </summary>
public class GreetingService : BaseRpcMethodHandler {
    private static readonly List<string> _greetings = new()
    {
        "Hello", "Hi", "Greetings", "Salutations", "Welcome", "Good day"
    };

    private static readonly Dictionary<string, string> _timeOfDayGreetings = new()
    {
        { "morning", "Good morning" },
        { "afternoon", "Good afternoon" },
        { "evening", "Good evening" },
        { "night", "Good night" }
    };

    /// <summary>
    /// Simple greeting with a name.
    /// Example call: "Alice" (single string parameter)
    /// </summary>
    [RpcMethod("SayHello", Description = "Says hello to someone")]
    public string SayHello(string name) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new InvalidParametersException("Name cannot be empty");
        }

        var greeting = _greetings[Random.Shared.Next(_greetings.Count)];
        var message = $"{greeting}, {name.Trim()}!";

        Console.WriteLine($"Greeting: Generated greeting for {name}");
        return message;
    }

    /// <summary>
    /// Personalized greeting with name and time of day.
    /// Example call: { "name": "Bob", "timeOfDay": "morning" }
    /// </summary>
    [RpcMethod("PersonalizedGreeting", Description = "Creates a personalized greeting")]
    public string PersonalizedGreeting(string name, string timeOfDay = "day") {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new InvalidParametersException("Name cannot be empty");
        }

        var greeting = _timeOfDayGreetings.ContainsKey(timeOfDay.ToLower())
            ? _timeOfDayGreetings[timeOfDay.ToLower()]
            : "Hello";

        var message = $"{greeting}, {name.Trim()}! Hope you're having a wonderful {timeOfDay}.";

        Console.WriteLine($"Greeting: Generated personalized greeting for {name} ({timeOfDay})");
        return message;
    }

    /// <summary>
    /// Gets the current server time.
    /// Example call: (no parameters)
    /// </summary>
    [RpcMethod("GetCurrentTime", Description = "Gets the current server time")]
    public DateTime GetCurrentTime() {
        var currentTime = DateTime.UtcNow;
        Console.WriteLine($"Greeting: Returning current time: {currentTime:yyyy-MM-dd HH:mm:ss} UTC");
        return currentTime;
    }

    /// <summary>
    /// Creates a greeting in a specific language.
    /// Example call: { "name": "Carlos", "language": "spanish" }
    /// </summary>
    [RpcMethod("MultiLanguageGreeting", Description = "Creates a greeting in different languages")]
    public string MultiLanguageGreeting(string name, string language = "english") {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new InvalidParametersException("Name cannot be empty");
        }

        var greetingTemplates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "english", "Hello, {0}!" },
            { "spanish", "¡Hola, {0}!" },
            { "french", "Bonjour, {0}!" },
            { "german", "Hallo, {0}!" },
            { "italian", "Ciao, {0}!" },
            { "portuguese", "Olá, {0}!" },
            { "russian", "Привет, {0}!" },
            { "japanese", "こんにちは, {0}!" },
            { "chinese", "你好, {0}!" }
        };

        if (!greetingTemplates.ContainsKey(language)) {
            throw new InvalidParametersException($"Language '{language}' is not supported",
                new { SupportedLanguages = greetingTemplates.Keys.ToArray() });
        }

        var message = string.Format(greetingTemplates[language], name.Trim());

        Console.WriteLine($"Greeting: Generated {language} greeting for {name}");
        return message;
    }

    /// <summary>
    /// Generates a birthday greeting.
    /// Example call: { "name": "Emma", "age": 25 }
    /// </summary>
    [RpcMethod("BirthdayGreeting", Description = "Creates a birthday greeting")]
    public string BirthdayGreeting(string name, int age) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new InvalidParametersException("Name cannot be empty");
        }

        if (age < 0 || age > 150) {
            throw new InvalidParametersException("Age must be between 0 and 150",
                new { ProvidedAge = age });
        }

        var message = age switch {
            0 => $"Welcome to the world, {name}!",
            1 => $"Happy 1st birthday, {name}!",
            _ when age < 13 => $"Happy {age}th birthday, {name}! Hope your special day is magical!",
            _ when age < 20 => $"Happy {age}th birthday, {name}! Enjoy your teenage years!",
            _ when age < 30 => $"Happy {age}th birthday, {name}! Your twenties are amazing!",
            _ when age < 50 => $"Happy {age}th birthday, {name}! Hope this year brings great adventures!",
            _ => $"Happy {age}th birthday, {name}! Wishing you health and happiness!"
        };

        Console.WriteLine($"Greeting: Generated birthday greeting for {name}, age {age}");
        return message;
    }

    /// <summary>
    /// Formats a name according to different styles.
    /// Example call: { "firstName": "john", "lastName": "doe", "style": "formal" }
    /// </summary>
    [RpcMethod("FormatName", Description = "Formats a name in different styles")]
    public string FormatName(string firstName, string lastName, string style = "normal") {
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName)) {
            throw new InvalidParametersException("First name and last name are required");
        }

        var first = firstName.Trim();
        var last = lastName.Trim();

        var formatted = style.ToLower() switch {
            "formal" => $"{last.ToUpper()}, {first.Substring(0, 1).ToUpper()}{first.Substring(1).ToLower()}",
            "casual" => $"{first.ToLower()} {last.ToLower()}",
            "initials" => $"{first.Substring(0, 1).ToUpper()}.{last.Substring(0, 1).ToUpper()}.",
            "reverse" => $"{last} {first}",
            "normal" or _ => $"{first.Substring(0, 1).ToUpper()}{first.Substring(1).ToLower()} {last.Substring(0, 1).ToUpper()}{last.Substring(1).ToLower()}"
        };

        Console.WriteLine($"Greeting: Formatted name '{firstName} {lastName}' as '{formatted}' (style: {style})");
        return formatted;
    }

    /// <summary>
    /// Generates a random compliment.
    /// Example call: "Alice" (single parameter)
    /// </summary>
    [RpcMethod("RandomCompliment", Description = "Generates a random compliment")]
    public string RandomCompliment(string name) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new InvalidParametersException("Name cannot be empty");
        }

        var compliments = new[]
        {
            "You're amazing",
            "You have a great sense of style",
            "You're incredibly talented",
            "You brighten everyone's day",
            "You have a wonderful smile",
            "You're very thoughtful",
            "You're a great friend",
            "You have excellent taste",
            "You're very creative",
            "You're absolutely fantastic"
        };

        var compliment = compliments[Random.Shared.Next(compliments.Length)];
        var message = $"{compliment}, {name.Trim()}!";

        Console.WriteLine($"Greeting: Generated compliment for {name}");
        return message;
    }

    /// <summary>
    /// Creates a farewell message.
    /// Example call: { "name": "David", "destination": "vacation" }
    /// </summary>
    [RpcMethod("Farewell", Description = "Creates a farewell message")]
    public string Farewell(string name, string destination = "") {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new InvalidParametersException("Name cannot be empty");
        }

        var message = string.IsNullOrWhiteSpace(destination)
            ? $"Goodbye, {name.Trim()}! Take care!"
            : $"Goodbye, {name.Trim()}! Have a wonderful time at {destination.Trim()}!";

        Console.WriteLine($"Greeting: Generated farewell for {name}" +
            (string.IsNullOrWhiteSpace(destination) ? "" : $" going to {destination}"));

        return message;
    }

    /// <summary>
    /// Gets service statistics.
    /// Example call: (no parameters)
    /// </summary>
    [RpcMethod("GetServiceInfo", Description = "Gets greeting service information")]
    public object GetServiceInfo() {
        Console.WriteLine("Greeting: Returning service information");
        return new {
            ServiceName = "GreetingService",
            Version = "1.0.0",
            SupportedLanguages = new[] { "english", "spanish", "french", "german", "italian", "portuguese", "russian", "japanese", "chinese" },
            AvailableGreetings = _greetings.ToArray(),
            TimeOfDayGreetings = _timeOfDayGreetings.Keys.ToArray(),
            ServerTime = DateTime.UtcNow,
            ServerName = Environment.MachineName
        };
    }
}
