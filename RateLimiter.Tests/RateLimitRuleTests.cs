using Xunit;
using RateLimiter.Services;

namespace RateLimiter.Tests;

public class RateLimitRuleTests
{
    //Checks if we can make the first call when nothing has happened yet
    [Fact]
    public async Task CanPerform_WhenNoCallsMade_ReturnsTrue()
    {
        //Arrange
        var rule = new RateLimitRule(1, TimeSpan.FromSeconds(1));

        //Act
        var result = await rule.CanPerform();

        //Assert
        Assert.True(result.canPerform);
        Assert.Equal(TimeSpan.Zero, result.delay);
    }

    //Checks if we get blocked when we try to make too many calls
    [Fact]
    public async Task CanPerform_WhenMaxCallsReached_ReturnsFalse()
    {
        //Arrange
        var rule = new RateLimitRule(1, TimeSpan.FromSeconds(1));
        var result1 = await rule.CanPerform();
        Assert.True(result1.canPerform);

        //Act
        var result2 = await rule.CanPerform();

        //Assert
        Assert.False(result2.canPerform);
        Assert.True(result2.delay > TimeSpan.Zero);
    }

    //Checks if we can make calls again after waiting for the time window to pass
    [Fact]
    public async Task CanPerform_WhenTimeWindowPassed_ReturnsTrue()
    {
        //Arrange
        var rule = new RateLimitRule(1, TimeSpan.FromMilliseconds(50));
        var result1 = await rule.CanPerform();
        Assert.True(result1.canPerform);
        
        await Task.Delay(100); //Wait for time window to pass

        //Act
        var result2 = await rule.CanPerform();

        //Assert
        Assert.True(result2.canPerform);
        Assert.Equal(TimeSpan.Zero, result2.delay);
    }

    //Checks if everything works correctly when multiple calls happen at the same time
    [Fact]
    public async Task CanPerform_MultipleConcurrentCalls_HandlesRaceConditions()
    {
        //Arrange
        var rule = new RateLimitRule(2, TimeSpan.FromSeconds(1));
        var tasks = new List<Task<(bool canPerform, TimeSpan delay)>>();

        //Act
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(rule.CanPerform());
        }

        var results = await Task.WhenAll(tasks);

        //Assert
        Assert.Equal(2, results.Count(r => r.canPerform));
        Assert.Equal(3, results.Count(r => !r.canPerform));
    }

    //Checks if old timestamps get cleaned up properly after they expire
    [Fact]
    public async Task CanPerform_CleanupOfExpiredTimestamps()
    {
        //Arrange
        var rule = new RateLimitRule(2, TimeSpan.FromMilliseconds(100));
        
        //Make initial calls
        var result1 = await rule.CanPerform();
        var result2 = await rule.CanPerform();
        Assert.True(result1.canPerform);
        Assert.True(result2.canPerform);
        
        //Wait for window to expire
        await Task.Delay(150);
        
        //Act
        var result3 = await rule.CanPerform();
        
        //Assert
        Assert.True(result3.canPerform);
        Assert.Equal(TimeSpan.Zero, result3.delay);
    }
}
