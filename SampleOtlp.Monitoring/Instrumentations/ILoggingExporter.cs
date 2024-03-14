using OpenTelemetry.Logs;

namespace SampleOtlp.Monitoring;

public interface ILoggingExporter
{
    OpenTelemetryLoggerOptions AddExporter(OpenTelemetryLoggerOptions builder);
}