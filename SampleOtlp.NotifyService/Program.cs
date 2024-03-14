using System.Net;
using Infrastructure.Caching;
using Infrastructure.Caching.Redis;
using SampleOtlp.Monitoring;
using SampleOtlp.Monitoring.Queue;
using SampleOtlp.NotifyService;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // Monitoring
        var otlpOptions = services.BuildOptionMonitor(hostContext.Configuration);
        services
            .AddCoreMonitor(otlpOptions)
            .AddCoreTrace(otlpOptions)
            .AddCoreMetric(otlpOptions)
            .AddCoreLogging(otlpOptions);

        services.AddStackExchangeRedisCache(options =>
        {
            options.InstanceName = "TestOTEL";
            options.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions();
            options.ConfigurationOptions.EndPoints.Add("localhost:6379");
        });
        services.AddSingleton<ICacheService, RedisCacheService>();
        
        services.AddSingleton<MessageReceiver>();

        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
