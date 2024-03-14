using OpenTelemetry.Trace;

namespace SampleOtlp.Monitoring;

public interface ITraceInstrumentation
{
    TracerProviderBuilder AddInstrumentation(TracerProviderBuilder builder);
}