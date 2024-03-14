using SampleOtlp.Cognito;
using SampleOtlp.Monitoring;

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
