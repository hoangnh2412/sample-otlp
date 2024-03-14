using OpenTelemetry.Metrics;

namespace SampleOtlp.Monitoring;

public interface IMetricExporter
{
    MeterProviderBuilder AddExporter(MeterProviderBuilder builder);
}