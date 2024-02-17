using OpenTelemetry.Resources;
using System.Reflection;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using System.Diagnostics.Metrics;
using OpenTelemetry.Logs;
using Microsoft.Extensions.DependencyInjection;
using SampleOtlp.Monitoring.Exporters;
using SampleOtlp.Monitoring.Instrumentations;
using Microsoft.Extensions.Logging;

namespace SampleOtlp.Monitoring;

public static class MonitoringExtension
{
    public static IServiceCollection AddCoreMonitor(this IServiceCollection services, OTLPOption otlpOptions)
    {
        services
            .AddOpenTelemetry()
            .ConfigureResource(options =>
            {
                options
                    .AddEnvironmentVariableDetector()
                    .AddTelemetrySdk()
                    .AddService(
                        serviceName: string.IsNullOrEmpty(otlpOptions.ServiceName) ? Assembly.GetEntryAssembly().GetName().Name : otlpOptions.ServiceName,
                        serviceVersion: Assembly.GetEntryAssembly().GetName().Version?.ToString() ?? "unknown",
                        serviceInstanceId: string.IsNullOrEmpty(otlpOptions.ServiceInstanceId) ? Environment.MachineName : otlpOptions.ServiceInstanceId);
            });

        return services;
    }

    public static IServiceCollection AddCoreTrace(this IServiceCollection services, OTLPOption otlpOptions, params string[] sources)
    {
        if (otlpOptions.Tracing == null)
            return services;

        services
            .AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder.AddSource(Assembly.GetEntryAssembly().GetName().Name);
                if (sources != null && sources.Length > 0)
                    builder.AddSource(sources);

                builder
                    .SetResourceBuilder(BuildResource(otlpOptions))
                    .SetSampler(new AlwaysOnSampler())
                    .SetErrorStatusOnException(true)
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddSqlClientInstrumentation();

                // Intstrumentations
                foreach (var item in OTLPType.TraceInstrumentations)
                {
                    var exporterType = Type.GetType(item.Value);
                    var exporterInstance = Activator.CreateInstance(exporterType, otlpOptions) as ITraceInstrumentation;
                    exporterInstance.AddInstrumentation(builder);
                }

                // Exporters
                builder.AddConsoleExporter();
                if (otlpOptions.Tracing.Exporter == default(OTLPOption.ExporterType) || !OTLPType.TraceExporters.TryGetValue(otlpOptions.Tracing.Exporter, out string exporterTypeName))
                {
                    builder.AddConsoleExporter();
                }
                else
                {
                    var exporterType = Type.GetType(exporterTypeName);
                    var exporterInstance = Activator.CreateInstance(exporterType, otlpOptions) as ITraceExporter;
                    exporterInstance.AddExporter(builder);
                }
            });

        return services;
    }

    public static IServiceCollection AddCoreMetric(this IServiceCollection services, OTLPOption otlpOptions, params string[] meters)
    {
        if (otlpOptions.Metric == null)
            return services;

        services
            .AddOpenTelemetry()
            .WithMetrics(builder =>
            {

                builder.AddMeter(Assembly.GetEntryAssembly().GetName().Name);
                if (meters != null && meters.Length > 0)
                    builder.AddMeter(meters);

                builder
                    .SetResourceBuilder(BuildResource(otlpOptions))
                    .AddRuntimeInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation();

                switch (otlpOptions.HistogramAggregation)
                {
                    case "exponential":
                        builder.AddView(instrument =>
                        {
                            return instrument.GetType().GetGenericTypeDefinition() == typeof(Histogram<>)
                                ? new Base2ExponentialBucketHistogramConfiguration()
                                : null;
                        });
                        break;
                    default:
                        break;
                }

                // Instrumentations
                foreach (var item in OTLPType.MetricInstrumentations)
                {
                    var exporterType = Type.GetType(item.Value);
                    var exporterInstance = Activator.CreateInstance(exporterType, otlpOptions) as IMetricInstrumentation;
                    exporterInstance.AddInstrumentation(builder);
                }

                // Exporters
                if (otlpOptions.Metric.Exporter == default(OTLPOption.ExporterType) || !OTLPType.MetricExporters.TryGetValue(otlpOptions.Metric.Exporter, out string exporterTypeName))
                {
                    builder.AddConsoleExporter();
                }
                else
                {
                    var exporterType = Type.GetType(exporterTypeName);
                    var exporterInstance = Activator.CreateInstance(exporterType, otlpOptions) as IMetricExporter;
                    exporterInstance.AddExporter(builder);
                }
            });

        return services;
    }

    public static IServiceCollection AddCoreLogging(this IServiceCollection services, OTLPOption otlpOptions)
    {
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.Configure(options =>
            {
                options.ActivityTrackingOptions = ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId;
            });

            builder.AddOpenTelemetry(options =>
            {
                options.IncludeFormattedMessage = true;
                options.IncludeScopes = true;
                options.ParseStateValues = true;

                options.SetResourceBuilder(BuildResource(otlpOptions));

                // Exporters
                options.AddConsoleExporter();
                if (otlpOptions.Logging.Exporter == default(OTLPOption.ExporterType) || !OTLPType.LoggingExporters.TryGetValue(otlpOptions.Logging.Exporter, out string exporterTypeName))
                {
                    options.AddConsoleExporter();
                }
                else
                {
                    var exporterType = Type.GetType(exporterTypeName);
                    var exporterInstance = Activator.CreateInstance(exporterType, otlpOptions) as ILoggingExporter;
                    exporterInstance.AddExporter(options);
                }
            });
        });

        return services;
    }

    private static ResourceBuilder BuildResource(OTLPOption otlpOptions)
    {
        return ResourceBuilder
            .CreateDefault()
            .AddEnvironmentVariableDetector()
            .AddTelemetrySdk()
            .AddService(
                serviceName: string.IsNullOrEmpty(otlpOptions.ServiceName) ? Assembly.GetEntryAssembly().GetName().Name : otlpOptions.ServiceName,
                serviceVersion: Assembly.GetEntryAssembly().GetName().Version?.ToString() ?? "unknown",
                serviceInstanceId: string.IsNullOrEmpty(otlpOptions.ServiceInstanceId) ? Environment.MachineName : otlpOptions.ServiceInstanceId);
    }
}