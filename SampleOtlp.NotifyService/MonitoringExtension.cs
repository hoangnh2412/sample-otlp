using SampleOtlp.Monitoring;

namespace SampleOtlp.NotifyService;

public static class MonitoringExtension
{
    public static OTLPOption BuildOptionMonitor(this IServiceCollection services, IConfiguration configuration)
    {
        var otlpOptions = new OTLPOption();
        var otlpSection = configuration.GetSection("OTLP");
        services.Configure<OTLPOption>(otlpSection);
        otlpSection.Bind(otlpOptions);

        OTLPType.TraceInstrumentations.Add(OTLPOption.InstrumentationType.Redis, typeof(RedisTraceInstrumentation).AssemblyQualifiedName);
        // OTLPType.TraceInstrumentations.Add(OTLPOption.InstrumentationType.Elasticsearch, typeof(ElasticsearchTraceInstrumentation).AssemblyQualifiedName);

        OTLPType.TraceExporters.Add(OTLPOption.ExporterType.Uptrace, typeof(UptraceTraceExporter).AssemblyQualifiedName);
        OTLPType.MetricExporters.Add(OTLPOption.ExporterType.Uptrace, typeof(UptraceMetricExporter).AssemblyQualifiedName);
        OTLPType.LoggingExporters.Add(OTLPOption.ExporterType.Uptrace, typeof(UptraceLogExporter).AssemblyQualifiedName);

        return otlpOptions;
    }
}