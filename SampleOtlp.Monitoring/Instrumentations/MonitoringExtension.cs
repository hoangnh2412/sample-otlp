using OpenTelemetry.Resources;
using System.Reflection;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using System.Diagnostics.Metrics;
using OpenTelemetry.Logs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SampleOtlp.Monitoring;

public static class MonitoringExtension
{
    public static Uri ParseUptraceDsn(string dsn)
    {
        Uri otlpGrpcEndpoint = null;

        var uri = new Uri(dsn);
        if (uri.Host == "uptrace.dev" || uri.Host == "api.uptrace.dev")
        {
            otlpGrpcEndpoint = new Uri("https://otlp.uptrace.dev:4317");
        }
        else
        {
            var grpcPort = 14317;

            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            Int32.TryParse(query["grpc"], out grpcPort);

            otlpGrpcEndpoint =
                new UriBuilder
                {
                    Scheme = uri.Scheme,
                    Host = uri.DnsSafeHost,
                    Port = grpcPort,
                }.Uri;
        }

        return otlpGrpcEndpoint;
    }

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
            .WithTracing(configure =>
            {
                configure.AddSource(Assembly.GetEntryAssembly().GetName().Name);
                if (sources != null && sources.Length > 0)
                    configure.AddSource(sources);

                configure
                    .SetResourceBuilder(BuildResource(otlpOptions))
                    .SetSampler(new AlwaysOnSampler())
                    .SetErrorStatusOnException(true)
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;

                        options.EnrichWithHttpRequest = (activity, request) =>
                        {
                            activity.SetTag("app.identity.userId", Guid.NewGuid());
                            activity.SetTag("app.identity.username", "hoangnh");
                        };

                        options.EnrichWithHttpResponse = (activity, response) =>
                        {
                            activity.SetTag("app.identity.userId", Guid.NewGuid());
                            activity.SetTag("app.identity.username", "hoangnh");
                        };

                        options.EnrichWithException = (activity, exception) =>
                        {
                            activity.SetTag("app.identity.userId", Guid.NewGuid());
                            activity.SetTag("app.identity.username", "hoangnh");
                        };
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;

                        options.EnrichWithHttpRequestMessage = (activity, request) =>
                        {
                            activity.SetTag("app.identity.userId", Guid.NewGuid());
                            activity.SetTag("app.identity.username", "hoangnh");
                        };

                        options.EnrichWithHttpResponseMessage = (activity, response) =>
                        {
                            activity.SetTag("app.identity.userId", Guid.NewGuid());
                            activity.SetTag("app.identity.username", "hoangnh");
                        };

                        options.EnrichWithException = (activity, exception) =>
                        {
                            activity.SetTag("app.identity.userId", Guid.NewGuid());
                            activity.SetTag("app.identity.username", "hoangnh");
                        };
                    })
                    .AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        options.SetDbStatementForText = true;

                        // options.EnrichWithIDbCommand = (activity, command) =>
                        // {
                        //     command.
                        // };
                    })
                    .AddSqlClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.SetDbStatementForText = true;

                        // options.Enrich = (activity, )
                    });

                // Intstrumentations
                foreach (var item in OTLPType.TraceInstrumentations)
                {
                    var instance = Activator.CreateInstance(Type.GetType(item.Value), otlpOptions) as ITraceInstrumentation;
                    instance.AddInstrumentation(configure);
                }

                // Exporters
                configure.AddConsoleExporter();
                if (otlpOptions.Tracing.Exporter == default(OTLPOption.ExporterType) || !OTLPType.TraceExporters.TryGetValue(otlpOptions.Tracing.Exporter, out string exporterTypeName))
                    configure.AddConsoleExporter();
                else
                    (Activator.CreateInstance(Type.GetType(exporterTypeName), otlpOptions) as ITraceExporter).AddExporter(configure);
            });

        return services;
    }

    public static IServiceCollection AddCoreMetric(this IServiceCollection services, OTLPOption otlpOptions, params string[] meters)
    {
        if (otlpOptions.Metric == null)
            return services;

        services
            .AddOpenTelemetry()
            .WithMetrics(configure =>
            {
                if (meters != null && meters.Length > 0)
                    configure.AddMeter(meters);

                configure
                    .SetResourceBuilder(BuildResource(otlpOptions))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation();

                switch (otlpOptions.HistogramAggregation)
                {
                    case "exponential":
                        configure.AddView(instrument =>
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
                    var instance = Activator.CreateInstance(Type.GetType(item.Value), otlpOptions) as IMetricInstrumentation;
                    instance.AddInstrumentation(configure);
                }

                // Exporters
                if (otlpOptions.Metric.Exporter == default(OTLPOption.ExporterType) || !OTLPType.MetricExporters.TryGetValue(otlpOptions.Metric.Exporter, out string exporterTypeName))
                    configure.AddConsoleExporter();
                else
                    (Activator.CreateInstance(Type.GetType(exporterTypeName), otlpOptions) as IMetricExporter).AddExporter(configure);
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
                // options.AddConsoleExporter();
                if (otlpOptions.Logging.Exporter == default(OTLPOption.ExporterType) || !OTLPType.LoggingExporters.TryGetValue(otlpOptions.Logging.Exporter, out string exporterTypeName))
                    options.AddConsoleExporter();
                else
                    (Activator.CreateInstance(Type.GetType(exporterTypeName), otlpOptions) as ILoggingExporter).AddExporter(options);
            });
        });

        return services;
    }

    private static ResourceBuilder BuildResource(OTLPOption otlpOptions)
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (string.IsNullOrEmpty(env))
            env = "Development";

        return ResourceBuilder
            .CreateDefault()
            .AddEnvironmentVariableDetector()
            .AddTelemetrySdk()
            .AddAttributes(new List<KeyValuePair<string, object>> {
                new KeyValuePair<string, object>("deployment.environment", env)
            })
            .AddService(
                serviceName: string.IsNullOrEmpty(otlpOptions.ServiceName) ? Assembly.GetEntryAssembly().GetName().Name : otlpOptions.ServiceName,
                serviceVersion: Assembly.GetEntryAssembly().GetName().Version?.ToString() ?? "unknown",
                serviceInstanceId: string.IsNullOrEmpty(otlpOptions.ServiceInstanceId) ? Environment.MachineName : otlpOptions.ServiceInstanceId);
    }
}