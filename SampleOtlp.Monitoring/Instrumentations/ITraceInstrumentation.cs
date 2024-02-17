using OpenTelemetry.Trace;

namespace SampleOtlp.Monitoring.Instrumentations;

public interface ITraceInstrumentation
{
    TracerProviderBuilder AddInstrumentation(TracerProviderBuilder builder);
}