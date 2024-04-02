using OpenTelemetry.Logs;

namespace SampleOtlp.Monitoring;

public class OTLPLogExporter : ILoggingExporter
{
    private readonly OTLPOption _options;

    public OTLPLogExporter(
        OTLPOption options)
    {
        _options = options;
    }

    public OpenTelemetryLoggerOptions AddExporter(OpenTelemetryLoggerOptions builder)
    {
        builder.AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(_options.Logging.Endpoint);
        });
        return builder;
    }
}