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
    Console.WriteLine($"Running API call to {url} at {DateTime.Now:HH:mm:ss.fff}");
    await Task.Delay(100); 
}, rulesList);

var tasks = new List<Task>();

for (int i = 0; i < 15; i++)
{
    int captured = i;
    tasks.Add(Task.Run(() => rateLimiter.Perform(captured.ToString())));
}

await Task.WhenAll(tasks);
Console.ReadLine(); // Keep the app open