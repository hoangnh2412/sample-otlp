using OpenTelemetry.Logs;

namespace SampleOtlp.Monitoring.Exporters;

public interface ILoggingExporter
{
    OpenTelemetryLoggerOptions AddExporter(OpenTelemetryLoggerOptions builder);
}