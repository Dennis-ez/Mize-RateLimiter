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

var tasks = new List<Task>();

for (int i = 0; i < 15; i++)
{
    tasks.Add(Task.Run(() => rateLimiter.Perform(i.ToString())));
}

await Task.WhenAll(tasks);
Console.ReadLine(); //keep the app open