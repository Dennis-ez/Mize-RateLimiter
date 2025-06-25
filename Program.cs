using RateLimiter.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

var rulesList = new List<RateLimitRule>
{
    new RateLimitRule(1, TimeSpan.FromSeconds(5)),
    new RateLimitRule(2, TimeSpan.FromSeconds(15)),
    new RateLimitRule(3, TimeSpan.FromSeconds(10))
};

var rateLimiter = new RateLimiter<string>(async (url) =>
{
    //simulate running a task that takes 100 milliseconds
    await Task.Delay(100); 
}, rulesList);

    for (int i = 0; i < 15; i++)
    {
        await rateLimiter.Perform(i.ToString());
    }

Console.ReadLine(); //keep the app openß