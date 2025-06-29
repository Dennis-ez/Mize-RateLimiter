using RateLimiter.Services;

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
    int requestId = i;
    tasks.Add(Task.Run(async () =>
    {
        await rateLimiter.Perform(requestId.ToString());
    }));
}

await Task.WhenAll(tasks);
Console.WriteLine("All tasks completed");
Console.ReadLine(); //keep the app open
