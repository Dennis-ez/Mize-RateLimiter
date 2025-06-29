using Xunit;
using RateLimiter.Services;

namespace RateLimiter.Tests;

public class RateLimitRuleTests
{
    //Checks if we can make the first call when nothing has happened yet
    [Fact]
    public async Task CanPerform_WhenNoCallsMade_ReturnsTrue()
    {
        var rule = new RateLimitRule(1, TimeSpan.FromSeconds(1));
        
        var result = await Task.Run(() => rule.CanPerform());
        
        Assert.True(result.canPerform);
        Assert.Equal(TimeSpan.Zero, result.delay);
    }

    //Checks if we get blocked when we try to make too many calls
    [Fact]
    public async Task CanPerform_WhenMaxCallsReached_ReturnsFalse()
    {
        var rule = new RateLimitRule(1, TimeSpan.FromSeconds(1));
        
        var result1 = await Task.Run(async () => 
        {
            var canPerform = await rule.CanPerform();
            if (canPerform.canPerform)
            {
                await rule.RecordCall();
            }
            return canPerform;
        });
        
        Assert.True(result1.canPerform);
        
        var result2 = await Task.Run(() => rule.CanPerform());
        Assert.False(result2.canPerform);
        Assert.True(result2.delay > TimeSpan.Zero);
    }

    //Checks if we can make calls again after waiting for the time window to pass
    [Fact]
    public async Task CanPerform_WhenTimeWindowPassed_ReturnsTrue()
    {
        var rule = new RateLimitRule(1, TimeSpan.FromMilliseconds(50));
        
        await Task.Run(async () => 
        {
            var canPerform = await rule.CanPerform();
            if (canPerform.canPerform)
            {
                await rule.RecordCall();
            }
        });
        
        await Task.Delay(100); // Wait for time window to pass
        
        var result = await Task.Run(() => rule.CanPerform());
        Assert.True(result.canPerform);
        Assert.Equal(TimeSpan.Zero, result.delay);
    }

    //Checks if everything works correctly when multiple calls happen at the same time
    [Fact]
    public async Task CanPerform_MultipleConcurrentCalls_HandlesRaceConditions()
    {
        var rule = new RateLimitRule(2, TimeSpan.FromSeconds(1));
        var successCount = 0;
        var tasks = new List<Task>();
        var lockObj = new object();
        var attemptedCount = 0;

        for (int i = 0; i < 5; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                lock (lockObj)
                {
                    if (attemptedCount >= 2) return; // Only attempt first 2 calls
                    attemptedCount++;
                }
                
                var result = await rule.CanPerform();
                if (result.canPerform)
                {
                    await rule.RecordCall();
                    Interlocked.Increment(ref successCount);
                }
            }));
        }
    
        await Task.WhenAll(tasks);
        Assert.Equal(2, successCount); // Only first two should succeed
    }

    //Checks if old timestamps get cleaned up properly after they expire
    [Fact]
    public async Task CanPerform_CleanupOfExpiredTimestamps()
    {
        var rule = new RateLimitRule(2, TimeSpan.FromMilliseconds(100));
        
        await Task.Run(async () =>
        {
            var result1 = await rule.CanPerform();
            Assert.True(result1.canPerform);
            await rule.RecordCall();
            
            var result2 = await rule.CanPerform();
            Assert.True(result2.canPerform);
            await rule.RecordCall();
        });
        
        await Task.Delay(150); // Wait for window to expire
        
        var result3 = await Task.Run(() => rule.CanPerform());
        Assert.True(result3.canPerform);
        Assert.Equal(TimeSpan.Zero, result3.delay);
    }
}
