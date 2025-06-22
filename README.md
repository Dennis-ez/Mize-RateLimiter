# Rate Limiter


## Quick Start

```csharp
// Set up rules
var rules = new List<RateLimitRule>
{
    new RateLimitRule(maxCalls: 1, TimeSpan.FromSeconds(5)),  // Allow 1 call every 5 seconds
    new RateLimitRule(maxCalls: 2, TimeSpan.FromMinutes(1)),  // Allow 2 calls per minute
};

// Create the rate limiter
var rateLimiter = new RateLimiter<string>(async (url) =>
{
    // Your code here
    await Task.Delay(100); 
}, rules);

// Use it
await rateLimiter.Perform("example");
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
dotnet test
```

## Requirements

- .NET 7.0
- C# 7.0 or newer
