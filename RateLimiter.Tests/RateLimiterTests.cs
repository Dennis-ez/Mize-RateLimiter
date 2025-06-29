using Xunit;
using RateLimiter.Services;

namespace RateLimiter.Tests;

public class RateLimiterTests
{
    //Checks if the action runs normally when we're within the rate limits
    [Fact]
    public async Task Perform_WithinLimits_ExecutesAction()
    {
        
        var executed = false;
        var rules = new List<RateLimitRule>
        {
            new RateLimitRule(1, TimeSpan.FromSeconds(1))
        };
        
        var rateLimiter = new RateLimiter<string>(async (arg) =>
        {
            executed = true;
            await Task.CompletedTask;
        }, rules);

        await Task.Run(() => rateLimiter.Perform("test"));
        await Task.Delay(100);
        
        Assert.True(executed);
    }

    //Checks if the second call gets delayed when we hit the rate limit
    [Fact]
    public async Task Perform_ExceedsLimit_DelaysExecution()
    {
        var executionTimes = new List<DateTime>();
        var executionLock = new object();
        var rules = new List<RateLimitRule>
        {
            new RateLimitRule(1, TimeSpan.FromMilliseconds(500))
        };
       
        var rateLimiter = new RateLimiter<string>(async (arg) =>
        {
            lock (executionLock)
            {
                executionTimes.Add(DateTime.Now);
            }
            await Task.CompletedTask;
        }, rules);

        var tasks = new List<Task>();
        tasks.Add(Task.Run(() => rateLimiter.Perform("test1")));
        tasks.Add(Task.Run(() => rateLimiter.Perform("test2")));

        await Task.WhenAll(tasks);
        await Task.Delay(100);
       
        Assert.Single(executionTimes);
    }

    //Makes sure both rate limits work together (2 per second AND 1 per 500ms)
    [Fact]
    public async Task Perform_MultipleRules_RespectsAllLimits()
    {
        var executionTimes = new List<DateTime>();
        var executionLock = new object();
        var rules = new List<RateLimitRule>
        {
            new RateLimitRule(2, TimeSpan.FromSeconds(1)),
            new RateLimitRule(1, TimeSpan.FromMilliseconds(500))
        };

        var rateLimiter = new RateLimiter<string>(async (arg) =>
        {
            lock (executionLock)
            {
                executionTimes.Add(DateTime.Now);
            }
            await Task.CompletedTask;
        }, rules);

        await Task.Run(() => rateLimiter.Perform("test1"));
        
        var tasks = new List<Task>
        {
            Task.Run(() => rateLimiter.Perform("test2")),
            Task.Run(() => rateLimiter.Perform("test3"))
        };

        await Task.WhenAll(tasks);
        await Task.Delay(100);
        
        Assert.Single(executionTimes); 
    }

    //Makes sure we can't create a rate limiter with a null action
    [Fact]
    public async Task Perform_NullAction_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Task.FromResult(new RateLimiter<string>(null!, new List<RateLimitRule>())));
    }

    //Makes sure we can't create a rate limiter with null rules
    [Fact]
    public async Task Perform_NullRules_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Task.FromResult(new RateLimiter<string>(async (_) => await Task.CompletedTask, null!)));
    }
}
