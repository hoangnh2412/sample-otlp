using OpenTelemetry.Metrics;

namespace SampleOtlp.Monitoring;

public class UptraceMetricExporter : IMetricExporter
{
    private readonly OTLPOption _options;

    public UptraceMetricExporter(
        OTLPOption options)
    {
        _options = options;
    }

    public MeterProviderBuilder AddExporter(MeterProviderBuilder builder)
    {
        builder.AddOtlpExporter(options =>
        {
            options.Endpoint = MonitoringExtension.ParseUptraceDsn(_options.Tracing.Endpoint);
            options.Headers = string.Format("uptrace-dsn={0}", _options.Tracing.Endpoint);
        });
        return builder;
    }
}