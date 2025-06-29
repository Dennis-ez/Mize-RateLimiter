namespace RateLimiter.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class RateLimiter<TArg>
{
    private readonly Func<TArg, Task> _action;
    private readonly List<RateLimitRule> _rules;

    public RateLimiter(Func<TArg, Task> action, List<RateLimitRule> rules)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
    }

    public async Task Perform(TArg argument)
    {
        var maxDelay = TimeSpan.Zero;
        try
        {
            foreach (var rule in _rules)
            {
                var result = await rule.CanPerform();
                if (!result.canPerform)
                {
                    if (result.delay > maxDelay)
                        maxDelay = result.delay;
                }
            }

            if (maxDelay == TimeSpan.Zero)
            {
                foreach (var rule in _rules)
                {
                    await rule.RecordCall();
                }
                Console.WriteLine($"Running API call to {argument} at {DateTime.Now:HH:mm:ss.fff}");
                await _action(argument);
                return;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during rate limiting: {ex.Message}");
            return;
        }

        Console.WriteLine($"Rate limit exceeded. Delaying for {maxDelay.TotalMilliseconds:F3} ms before a new call can be made.");
        await Task.Delay(maxDelay);
    }
}