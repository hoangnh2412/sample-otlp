using OpenTelemetry.Trace;

namespace SampleOtlp.Monitoring.Exporters;

public interface ITraceExporter
{
    TracerProviderBuilder AddExporter(TracerProviderBuilder builder);
}