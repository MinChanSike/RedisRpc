# Example Service Documentation

This document provides detailed information about the example services included in the Redis RPC sample, demonstrating various patterns and capabilities.

## CalculatorService

A mathematical operations service demonstrating numerical computations and error handling.

### Available Methods

#### Add

Adds two numbers together.

**Parameters:**

- `a` (int): First number
- `b` (int): Second number

**Returns:** `int` - Sum of the two numbers

**Example Call:**

```csharp
var result = await client.SendRequestAsync<int>("calculator", "Add", new { a = 10, b = 5 });
// Result: 15
```

#### Subtract

Subtracts the second number from the first.

**Parameters:**

- `a` (int): First number
- `b` (int): Second number

**Returns:** `int` - Difference of the two numbers

**Example Call:**

```csharp
var result = await client.SendRequestAsync<int>("calculator", "Subtract", new { a = 10, b = 3 });
// Result: 7
```

#### Multiply

Multiplies two numbers together (with double precision).

**Parameters:**

- `x` (double): First number
- `y` (double): Second number

**Returns:** `double` - Product of the two numbers

**Example Call:**

```csharp
var result = await client.SendRequestAsync<double>("calculator", "Multiply", new { x = 3.14, y = 2.0 });
// Result: 6.28
```

#### Divide

Divides the first number by the second with error handling.

**Parameters:**

- `a` (double): Dividend
- `b` (double): Divisor

**Returns:** `double` - Quotient

**Throws:** `InvalidParametersException` if divisor is zero

**Example Call:**

```csharp
var result = await client.SendRequestAsync<double>("calculator", "Divide", new { a = 10, b = 2 });
// Result: 5.0
```

#### Power

Calculates the power of a number.

**Parameters:**

- `baseNumber` (double): Base number
- `exponent` (double): Exponent

**Returns:** `double` - Result of base^exponent

**Example Call:**

```csharp
var result = await client.SendRequestAsync<double>("calculator", "Power", new { baseNumber = 2, exponent = 3 });
// Result: 8.0
```

#### SquareRoot

Calculates the square root of a number.

**Parameters:**

- `number` (double): Number to calculate square root of

**Returns:** `double` - Square root

**Throws:** `InvalidParametersException` if number is negative

**Example Call:**

```csharp
var result = await client.SendRequestAsync<double>("calculator", "SquareRoot", 16);
// Result: 4.0
```

#### Calculate

Evaluates a simple mathematical expression (demonstration purposes).

**Parameters:**

- `expression` (string): Mathematical expression to evaluate

**Returns:** `object` - Result of the expression

**Supported expressions:** Basic arithmetic with +, -, \*, and some predefined expressions

**Example Call:**

```csharp
var result = await client.SendRequestAsync<dynamic>("calculator", "Calculate", new { expression = "2 * (3 + 4)" });
// Result: 14
```

#### GetStatistics

Returns usage statistics for the calculator service.

**Parameters:** None

**Returns:** `object` - Statistics including operation count and server information

**Example Call:**

```csharp
var stats = await client.SendRequestAsync<dynamic>("calculator", "GetStatistics");
```

**Example Response:**

```json
{
  "totalOperations": 42,
  "serviceStartTime": "2024-01-01T11:00:00.000Z",
  "serverName": "SERVER01"
}
```

#### ResetCounters

Resets operation counters (typically used as notification).

**Parameters:** None

**Returns:** `void`

**Example Call:**

```csharp
await client.SendNotificationAsync("calculator", "ResetCounters");
```

#### BatchCalculate

Performs multiple calculations in a single request.

**Parameters:**

- `operations` (array): Array of operation objects

**Operation Object Format:**

- `op` (string): Operation type ("add", "subtract", "multiply", "divide")
- `a` (double): First operand
- `b` (double): Second operand

**Returns:** `object` - Results array with individual operation results

**Example Call:**

```csharp
var batchResult = await client.SendRequestAsync<dynamic>("calculator", "BatchCalculate", new
{
    operations = new[]
    {
        new { op = "add", a = 1, b = 2 },
        new { op = "multiply", a = 3, b = 4 }
    }
});
```

## GreetingService

A text processing service demonstrating string manipulation and internationalization.

### Available Methods

#### SayHello

Simple greeting with a name.

**Parameters:**

- `name` (string): Name to greet

**Returns:** `string` - Greeting message

**Example Call:**

```csharp
var greeting = await client.SendRequestAsync<string>("greeting", "SayHello", "Alice");
// Result: "Hello, Alice!"
```

#### PersonalizedGreeting

Creates a personalized greeting with time of day.

**Parameters:**

- `name` (string): Name to greet
- `timeOfDay` (string, optional): Time of day ("morning", "afternoon", "evening", "night")

**Returns:** `string` - Personalized greeting message

**Example Call:**

```csharp
var greeting = await client.SendRequestAsync<string>("greeting", "PersonalizedGreeting",
    new { name = "Bob", timeOfDay = "morning" });
// Result: "Good morning, Bob! Hope you're having a wonderful morning."
```

#### GetCurrentTime

Gets the current server time.

**Parameters:** None

**Returns:** `DateTime` - Current UTC time

**Example Call:**

```csharp
var serverTime = await client.SendRequestAsync<DateTime>("greeting", "GetCurrentTime");
```

#### MultiLanguageGreeting

Creates a greeting in different languages.

**Parameters:**

- `name` (string): Name to greet
- `language` (string, optional): Language code (default: "english")

**Supported Languages:**

- english, spanish, french, german, italian, portuguese, russian, japanese, chinese

**Returns:** `string` - Localized greeting message

**Example Call:**

```csharp
var greeting = await client.SendRequestAsync<string>("greeting", "MultiLanguageGreeting",
    new { name = "Carlos", language = "spanish" });
// Result: "¡Hola, Carlos!"
```

#### BirthdayGreeting

Generates an age-appropriate birthday greeting.

**Parameters:**

- `name` (string): Name of birthday person
- `age` (int): Age (0-150)

**Returns:** `string` - Birthday greeting message

**Example Call:**

```csharp
var greeting = await client.SendRequestAsync<string>("greeting", "BirthdayGreeting",
    new { name = "Emma", age = 25 });
```

#### FormatName

Formats a name according to different styles.

**Parameters:**

- `firstName` (string): First name
- `lastName` (string): Last name
- `style` (string, optional): Formatting style

**Supported Styles:**

- "normal": John Doe
- "formal": DOE, John
- "casual": john doe
- "initials": J.D.
- "reverse": Doe John

**Returns:** `string` - Formatted name

**Example Call:**

```csharp
var formatted = await client.SendRequestAsync<string>("greeting", "FormatName",
    new { firstName = "john", lastName = "doe", style = "formal" });
// Result: "DOE, John"
```

#### RandomCompliment

Generates a random compliment.

**Parameters:**

- `name` (string): Name to compliment

**Returns:** `string` - Random compliment message

**Example Call:**

```csharp
var compliment = await client.SendRequestAsync<string>("greeting", "RandomCompliment", "Alice");
// Result: "You're amazing, Alice!"
```

#### Farewell

Creates a farewell message.

**Parameters:**

- `name` (string): Name to bid farewell
- `destination` (string, optional): Where they're going

**Returns:** `string` - Farewell message

**Example Call:**

```csharp
var farewell = await client.SendRequestAsync<string>("greeting", "Farewell",
    new { name = "David", destination = "vacation" });
// Result: "Goodbye, David! Have a wonderful time at vacation!"
```

#### GetServiceInfo

Gets service information and capabilities.

**Parameters:** None

**Returns:** `object` - Service metadata including supported languages and features

**Example Call:**

```csharp
var info = await client.SendRequestAsync<dynamic>("greeting", "GetServiceInfo");
```

## DataService

A data management service demonstrating CRUD operations and data processing.

### Available Methods

#### CreateUser

Creates a new user in the system.

**Parameters:**

- `name` (string): User's full name
- `email` (string): Valid email address
- `age` (int): User's age (0-150)

**Returns:** `User` - Created user object with assigned ID

**Example Call:**

```csharp
var user = await client.SendRequestAsync<dynamic>("data", "CreateUser",
    new { name = "John Doe", email = "john@example.com", age = 30 });
```

**Example Response:**

```json
{
  "id": 1,
  "name": "John Doe",
  "email": "john@example.com",
  "age": 30,
  "createdAt": "2024-01-01T12:00:00.000Z",
  "updatedAt": null
}
```

#### GetUser

Retrieves a user by ID.

**Parameters:**

- `userId` (int): User ID to retrieve

**Returns:** `User` - User object or null if not found

**Example Call:**

```csharp
var user = await client.SendRequestAsync<dynamic>("data", "GetUser", 1);
```

#### GetAllUsers

Gets all users in the system.

**Parameters:** None

**Returns:** `List<User>` - Array of all users

**Example Call:**

```csharp
var users = await client.SendRequestAsync<List<dynamic>>("data", "GetAllUsers");
```

#### UpdateUser

Updates an existing user's information.

**Parameters:**

- `userId` (int): User ID to update
- `name` (string, optional): New name
- `email` (string, optional): New email address
- `age` (int, optional): New age

**Returns:** `User` - Updated user object

**Example Call:**

```csharp
var updated = await client.SendRequestAsync<dynamic>("data", "UpdateUser",
    new { userId = 1, name = "Jane Doe", age = 25 });
```

#### DeleteUser

Deletes a user by ID.

**Parameters:**

- `userId` (int): User ID to delete

**Returns:** `bool` - True if user was deleted, false if not found

**Example Call:**

```csharp
var deleted = await client.SendRequestAsync<bool>("data", "DeleteUser", 1);
```

#### SearchUsers

Searches users by name or email.

**Parameters:**

- `query` (string): Search term
- `maxResults` (int, optional): Maximum results to return (default: 50)

**Returns:** `List<User>` - Matching users

**Example Call:**

```csharp
var results = await client.SendRequestAsync<List<dynamic>>("data", "SearchUsers",
    new { query = "john", maxResults = 10 });
```

#### ProcessData

Processes an array of integers with various operations.

**Parameters:**

- `data` (int[]): Array of integers to process
- `operation` (string): Operation to perform

**Supported Operations:**

- "sum": Sum all values
- "average": Calculate average
- "min": Find minimum value
- "max": Find maximum value
- "count": Count elements
- "sort": Sort ascending
- "reverse": Reverse order
- "distinct": Get unique values

**Returns:** `object` - Processing result with operation details

**Example Call:**

```csharp
var result = await client.SendRequestAsync<dynamic>("data", "ProcessData",
    new { data = new[] { 1, 2, 3, 4, 5 }, operation = "sum" });
```

**Example Response:**

```json
{
  "operation": "sum",
  "result": 15,
  "count": 5
}
```

#### LogActivity

Logs an activity (typically called as notification).

**Parameters:**

- `activity` (string): Activity description
- `userId` (int, optional): Associated user ID
- `timestamp` (DateTime, optional): Activity timestamp (default: current time)

**Returns:** `void`

**Example Call:**

```csharp
await client.SendNotificationAsync("data", "LogActivity",
    new { activity = "User login", userId = 123, timestamp = DateTime.UtcNow });
```

#### GetActivityLog

Gets recent activity log entries.

**Parameters:**

- `count` (int, optional): Number of entries to retrieve (default: 20)

**Returns:** `List<string>` - Recent log entries

**Example Call:**

```csharp
var logs = await client.SendRequestAsync<List<string>>("data", "GetActivityLog",
    new { count = 10 });
```

#### UpdateCache

Updates a cache entry (typically called as notification).

**Parameters:**

- `key` (string): Cache key
- `value` (object): Value to cache

**Returns:** `void`

**Example Call:**

```csharp
await client.SendNotificationAsync("data", "UpdateCache",
    new { key = "user_count", value = 42 });
```

#### GetCache

Retrieves a cache entry.

**Parameters:**

- `key` (string): Cache key to retrieve

**Returns:** `object` - Cached value or null if not found

**Example Call:**

```csharp
var value = await client.SendRequestAsync<dynamic>("data", "GetCache", "user_count");
```

#### GetAllCache

Gets all cache entries.

**Parameters:** None

**Returns:** `Dictionary<string, object>` - All cache entries

**Example Call:**

```csharp
var cache = await client.SendRequestAsync<Dictionary<string, object>>("data", "GetAllCache");
```

#### GetServiceStats

Gets comprehensive service statistics.

**Parameters:** None

**Returns:** `object` - Service statistics and information

**Example Call:**

```csharp
var stats = await client.SendRequestAsync<dynamic>("data", "GetServiceStats");
```

**Example Response:**

```json
{
  "serviceName": "DataService",
  "version": "1.0.0",
  "statistics": {
    "totalUsers": 5,
    "cacheEntries": 3,
    "activityLogEntries": 15,
    "nextUserId": 6
  },
  "serverInfo": {
    "serverTime": "2024-01-01T12:00:00.000Z",
    "serverName": "SERVER01",
    "processId": 1234
  }
}
```

## Common Error Scenarios

### Method Not Found

```csharp
try {
    await client.SendRequestAsync("calculator", "NonExistentMethod");
} catch (MethodNotFoundException ex) {
    // Handle method not found
}
```

### Invalid Parameters

```csharp
try {
    await client.SendRequestAsync("calculator", "Add", "invalid");
} catch (InvalidParametersException ex) {
    // Handle parameter validation errors
}
```

### Business Logic Errors

```csharp
try {
    await client.SendRequestAsync("calculator", "Divide", new { a = 10, b = 0 });
} catch (InvalidParametersException ex) {
    // Handle division by zero
}
```

### Timeout Errors

```csharp
try {
    await client.SendRequestAsync("data", "SlowOperation", null, 1000); // 1 second timeout
} catch (RpcTimeoutException ex) {
    // Handle timeout
}
```

## Integration Patterns

### Request/Response Pattern

```csharp
// Synchronous-style call with typed response
var result = await client.SendRequestAsync<int>("calculator", "Add", new { a = 5, b = 3 });
```

### Fire-and-Forget Pattern

```csharp
// Asynchronous notification (no response)
await client.SendNotificationAsync("data", "LogActivity",
    new { activity = "Background task completed" });
```

### Batch Operations

```csharp
// Process multiple items in a single call
var batchResult = await client.SendRequestAsync<dynamic>("calculator", "BatchCalculate", new
{
    operations = new[]
    {
        new { op = "add", a = 1, b = 2 },
        new { op = "subtract", a = 10, b = 5 },
        new { op = "multiply", a = 3, b = 4 }
    }
});
```

### Complex Object Handling

```csharp
// Create and retrieve complex objects
var user = await client.SendRequestAsync<dynamic>("data", "CreateUser",
    new { name = "Alice Smith", email = "alice@example.com", age = 28 });

var retrieved = await client.SendRequestAsync<dynamic>("data", "GetUser", user.id);
```

These examples demonstrate the full capabilities of the Redis RPC system, showing how to handle different data types, error conditions, and communication patterns.
