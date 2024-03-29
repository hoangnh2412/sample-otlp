using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using Uptrace.OpenTelemetry;

namespace SampleOtlp.Monitoring.Exporters;

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
        builder.AddUptrace(_options.Metric.Endpoint);
        return builder;
    }
}