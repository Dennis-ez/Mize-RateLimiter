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

    public void RecordCall()
    {
        _timeStamps.Enqueue(DateTime.Now);
    }

    public async Task<(bool canPerform, TimeSpan delay)> CanPerform()
    {
        try
        {
            await _semaphore.WaitAsync();
            var currentTime = DateTime.Now;
            while (_timeStamps.TryPeek(out var firstTimeStamp) && (currentTime - firstTimeStamp >= TimeWindow))
            {
                _timeStamps.Dequeue();
            }

            if (_timeStamps.Count >= _maxCalls)
            {
                return (false, currentTime - _timeStamps.Peek());
            }

            //Record the call immediately when we determine it can be performed
            _timeStamps.Enqueue(currentTime);
            return (true, TimeSpan.Zero);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}