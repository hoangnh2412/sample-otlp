using Infrastructure.Caching;
using Infrastructure.Caching.Redis;
using SampleOtlp.Monitoring;
using SampleOtlp.SNS;

var builder = WebApplication.CreateBuilder(args);

// Monitoring
var otlpOptions = builder.Services.BuildOptionMonitor(builder.Configuration);
builder.Services
    .AddCoreMonitor(otlpOptions)
    .AddCoreTrace(otlpOptions)
    .AddCoreMetric(otlpOptions)
    .AddCoreLogging(otlpOptions);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.InstanceName = "TestOTEL";
    options.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions();
    options.ConfigurationOptions.EndPoints.Add("localhost:6379");
});
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
