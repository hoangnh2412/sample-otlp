using OpenTelemetry.Logs;

namespace SampleOtlp.Monitoring;

public class UptraceLogExporter : ILoggingExporter
{
    private readonly OTLPOption _options;

    public UptraceLogExporter(
        OTLPOption options)
    {
        _options = options;
    }

    public OpenTelemetryLoggerOptions AddExporter(OpenTelemetryLoggerOptions builder)
    {
        builder.AddOtlpExporter(options =>
        {
            options.Endpoint = MonitoringExtension.ParseUptraceDsn(_options.Tracing.Endpoint);
            options.Headers = string.Format("uptrace-dsn={0}", _options.Tracing.Endpoint);
        });
        return builder;
    }
}