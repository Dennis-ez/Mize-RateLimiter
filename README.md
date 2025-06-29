# Rate Limiter


## Quick Start

```csharp
// Set up rules
var rulesList = new List<RateLimitRule>
{
    new RateLimitRule(1, TimeSpan.FromSeconds(5)),    // Allow 1 call every 5 seconds
    new RateLimitRule(2, TimeSpan.FromSeconds(15)),   // Allow 2 calls every 15 seconds
    new RateLimitRule(3, TimeSpan.FromSeconds(10))    // Allow 3 calls every 10 seconds
};

// Create the rate limiter
var rateLimiter = new RateLimiter<string>(async (url) =>
{
    //simulate running a task that takes 100 milliseconds
    await Task.Delay(100); 
}, rulesList);

// Run multiple operations concurrently
var tasks = new List<Task>();
for (int i = 0; i < 15; i++)
{
    int requestId = i;
    tasks.Add(Task.Run(async () =>
    {
        await rateLimiter.Perform(requestId.ToString());
    }));
}

await Task.WhenAll(tasks);
```

## Sliding Window Approach

I use a sliding window approach because:

1. Instead of resetting limits at fixed times, it smoothly tracks recent calls
2. Looks at the exact time of each call
3. Automatically removes old timestamps it doesn't need anymore

## How It Works

1. Keeps track of when calls happen
2. When you try to make a call:
   - Checks all rules
   - If any rule would be broken, waits for the right amount of time
   - If all rules pass, runs your action
3. Cleans up old timestamps automatically

## Testing

Run tests with:
```bash
dotnet test RateLimiter.sln
```

## Requirements

- .NET 7.0
- C# 7.0 or newer
