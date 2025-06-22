namespace RateLimiter.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class RateLimiter<TArg>
{
    private readonly Func<TArg, Task> _action;
    private readonly List<RateLimitRule> _rules;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public RateLimiter(Func<TArg, Task> action, List<RateLimitRule> rules)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
    }

    public async Task Perform(TArg argument)
    {
        while (true)
        {
            var maxDelay = TimeSpan.Zero;
            var allPassed = true;

            await _semaphore.WaitAsync();
            try
            {
                foreach (var rule in _rules)
                {
                    var result = await rule.CanPerform();
                    if (!result.canPerform)
                    {
                        allPassed = false;
                        if (result.delay > maxDelay)
                            maxDelay = result.delay;
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }

            if (allPassed)
                break; //break out of loop to run action

            Console.WriteLine($"Delaying for {maxDelay.TotalMilliseconds} ms");
            await Task.Delay(maxDelay);
        }
        Console.WriteLine($"Running API call to {argument} at {DateTime.Now:HH:mm:ss.fff}");
        await _action(argument);
    }
}