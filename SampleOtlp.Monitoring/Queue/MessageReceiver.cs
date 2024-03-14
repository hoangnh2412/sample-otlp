using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Infrastructure.Caching;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace SampleOtlp.Monitoring.Queue;

public class MessageReceiver : IDisposable
{
    private static readonly ActivitySource ActivitySource = new ActivitySource(Assembly.GetEntryAssembly().GetName().Name);
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

    private readonly ICacheService _cacheService;
    private readonly ILogger<MessageReceiver> logger;
    private readonly IConnection connection;
    private readonly IModel channel;

    public MessageReceiver(
        ICacheService cacheService,
        ILogger<MessageReceiver> logger)
    {
        _cacheService = cacheService;
        this.logger = logger;
        this.connection = RabbitMqHelper.CreateConnection();
        this.channel = RabbitMqHelper.CreateModelAndDeclareTestQueue(this.connection);
    }

    public void Dispose()
    {
        this.channel.Dispose();
        this.connection.Dispose();
    }

    public void StartConsumer()
    {
        RabbitMqHelper.StartConsumer(this.channel, this.ReceiveMessage);
    }

    public void ReceiveMessage(BasicDeliverEventArgs ea)
    {
        // Extract the PropagationContext of the upstream parent from the message headers.
        var parentContext = Propagator.Extract(default, ea.BasicProperties, this.ExtractTraceContextFromBasicProperties);
        Baggage.Current = parentContext.Baggage;

        // Start an activity with a name following the semantic convention of the OpenTelemetry messaging specification.
        // https://github.com/open-telemetry/semantic-conventions/blob/main/docs/messaging/messaging-spans.md#span-name
        var activityName = $"{ea.RoutingKey} - {ea.Exchange} - {RabbitMqHelper.TestQueueName}: receive";

        using var activity = ActivitySource.StartActivity(activityName, ActivityKind.Consumer, parentContext.ActivityContext);
        try
        {
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());

            this.logger.LogInformation($"Message received: [{message}]");

            activity?.SetTag("message", message);

            // The OpenTelemetry messaging specification defines a number of attributes. These attributes are added here.
            RabbitMqHelper.AddMessagingTags(activity);

            using (var httpClientHandler = new HttpClientHandler())
            {
                httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };
                using (var cognitoClient = new HttpClient(httpClientHandler))
                {
                    var response = cognitoClient.PostAsync(new Uri("https://localhost:7120/sns"), new StringContent(message, System.Text.Encoding.UTF8, "application/json")).Result;
                    var content = response.Content.ReadAsStringAsync().Result;
                    if (!response.IsSuccessStatusCode)
                        throw new Exception($"{response.StatusCode} - {content}");
                }
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, ex.Message);
        }
    }

    public void StartConsumer2()
    {
        RabbitMqHelper.StartConsumer(this.channel, this.ReceiveMessage2);
    }

    public void ReceiveMessage2(BasicDeliverEventArgs ea)
    {
        // Extract the PropagationContext of the upstream parent from the message headers.
        var parentContext = Propagator.Extract(default, ea.BasicProperties, this.ExtractTraceContextFromBasicProperties);
        Baggage.Current = parentContext.Baggage;

        // Start an activity with a name following the semantic convention of the OpenTelemetry messaging specification.
        // https://github.com/open-telemetry/semantic-conventions/blob/main/docs/messaging/messaging-spans.md#span-name
        var activityName = $"{ea.RoutingKey} - {ea.Exchange} - {RabbitMqHelper.TestQueueName}: receive";

        using var activity = ActivitySource.StartActivity(activityName, ActivityKind.Consumer, parentContext.ActivityContext);
        try
        {
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());

            this.logger.LogInformation($"Message received: [{message}]");

            activity?.SetTag("message", message);

            // The OpenTelemetry messaging specification defines a number of attributes. These attributes are added here.
            RabbitMqHelper.AddMessagingTags(activity);

            var obj = JsonConvert.DeserializeObject<dynamic>(message);
            _cacheService.SetAsync($"sync:{obj["Id"].ToString()}", ea.Body, null).Wait();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, ex.Message);
        }
    }

    private IEnumerable<string> ExtractTraceContextFromBasicProperties(IBasicProperties props, string key)
    {
        try
        {
            if (props.Headers.TryGetValue(key, out var value))
            {
                var bytes = value as byte[];
                return new[] { Encoding.UTF8.GetString(bytes) };
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to extract trace context.");
        }

        return Enumerable.Empty<string>();
    }
}