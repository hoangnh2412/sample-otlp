using OpenTelemetry.Trace;

namespace SampleOtlp.Monitoring;

public class UptraceTraceExporter : ITraceExporter
{
    private readonly OTLPOption _options;

    public UptraceTraceExporter(
        OTLPOption options)
    {
        _options = options;
    }

    public TracerProviderBuilder AddExporter(TracerProviderBuilder builder)
    {
        builder.AddOtlpExporter(options =>
        {
            options.Endpoint = MonitoringExtension.ParseUptraceDsn(_options.Tracing.Endpoint);
            options.Headers = string.Format("uptrace-dsn={0}", _options.Tracing.Endpoint);
        });
        return builder;
    }
}