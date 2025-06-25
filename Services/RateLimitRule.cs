using System.Diagnostics.CodeAnalysis;

namespace RateLimiter.Services;
public class RateLimitRule
{
    private readonly int _maxCalls;
    public TimeSpan TimeWindow;
    private readonly Queue<DateTime> _timeStamps = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public RateLimitRule(int maxCalls, TimeSpan timeWindow)
    {
        _maxCalls = maxCalls;
        TimeWindow = timeWindow;
    }

    public async Task RecordCall()
    {
        await _semaphore.WaitAsync();
        try
        {
            _timeStamps.Enqueue(DateTime.UtcNow);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<(bool canPerform, TimeSpan delay)> CanPerform()
    {
        await _semaphore.WaitAsync();
        try
        {
            var currentTime = DateTime.UtcNow;
            
            // Clean up expired timestamps
            while (_timeStamps.TryPeek(out var firstTimeStamp) && (currentTime - firstTimeStamp >= TimeWindow))
            {
                _timeStamps.Dequeue();
            }

            // Check if we can make another call
            if (_timeStamps.Count >= _maxCalls)
            {
                var oldestTimestamp = _timeStamps.Peek();
                var windowEndTime = oldestTimestamp + TimeWindow;
                var delay = windowEndTime - currentTime;
                return (false, delay > TimeSpan.Zero ? delay : TimeWindow);
            }

            // If we get here, we can make the call
            return (true, TimeSpan.Zero);
        }
        finally
        {
            _semaphore.Release();
        }
    }

}