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

        
        await rateLimiter.Perform("test");

        
        Assert.True(executed);
    }

    //Checks if the second call gets delayed when we hit the rate limit
    [Fact]
    public async Task Perform_ExceedsLimit_DelaysExecution()
    {
        var executionTimes = new List<DateTime>();
        var rules = new List<RateLimitRule>
        {
            new RateLimitRule(1, TimeSpan.FromMilliseconds(500))
        };
        
        var rateLimiter = new RateLimiter<string>(async (arg) =>
        {
            executionTimes.Add(DateTime.Now);
            await Task.CompletedTask;
        }, rules);
        
        var task1 = rateLimiter.Perform("test1");
        var task2 = rateLimiter.Perform("test2");
        await Task.WhenAll(task1, task2);
        
        Assert.Equal(2, executionTimes.Count);
        var timeDifference = executionTimes[1] - executionTimes[0];
        Assert.True(timeDifference.TotalMilliseconds >= 500);
    }

    //Makes sure both rate limits work together (2 per second AND 1 per 500ms)
    [Fact]
    public async Task Perform_MultipleRules_RespectsAllLimits()
    {
        var executionTimes = new List<DateTime>();
        var rules = new List<RateLimitRule>
        {
            new RateLimitRule(2, TimeSpan.FromSeconds(1)),
            new RateLimitRule(1, TimeSpan.FromMilliseconds(500))
        };
        
        var rateLimiter = new RateLimiter<string>(async (arg) =>
        {
            executionTimes.Add(DateTime.Now);
            await Task.CompletedTask;
        }, rules);
        
        var tasks = new List<Task>();
        for (int i = 0; i < 3; i++)
        {
            tasks.Add(rateLimiter.Perform($"test{i}"));
        }
        await Task.WhenAll(tasks);

        
        Assert.Equal(3, executionTimes.Count);
        for (int i = 1; i < executionTimes.Count; i++)
        {
            var timeDiff = executionTimes[i] - executionTimes[i - 1];
            Assert.True(timeDiff.TotalMilliseconds >= 500);
        }
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
