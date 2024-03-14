using OpenTelemetry.Trace;

namespace SampleOtlp.Monitoring;

public interface ITraceExporter
{
    TracerProviderBuilder AddExporter(TracerProviderBuilder builder);
}