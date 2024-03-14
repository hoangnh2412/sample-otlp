using OpenTelemetry.Metrics;

namespace SampleOtlp.Monitoring;

public interface IMetricInstrumentation
{
    MeterProviderBuilder AddInstrumentation(MeterProviderBuilder builder);
}