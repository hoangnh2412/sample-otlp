using OpenTelemetry.Metrics;

namespace SampleOtlp.Monitoring.Exporters;

public interface IMetricExporter
{
    MeterProviderBuilder AddExporter(MeterProviderBuilder builder);
}