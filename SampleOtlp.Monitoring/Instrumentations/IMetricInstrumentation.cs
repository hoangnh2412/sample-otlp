using OpenTelemetry.Metrics;

namespace SampleOtlp.Monitoring.Instrumentations;

public interface IMetricInstrumentation
{
    MeterProviderBuilder AddInstrumentation(MeterProviderBuilder builder);
}