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
        bool allPassed;
        do
        {
            allPassed = true;
            var maxDelay = TimeSpan.Zero;
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

                if (allPassed)
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
            finally
            {
                _semaphore.Release();
            }

            Console.WriteLine($"Rate limit exceeded. Delaying for {maxDelay.TotalMilliseconds:F3} ms before retry");
            await Task.Delay(maxDelay);
        } while (!allPassed);
    }}

