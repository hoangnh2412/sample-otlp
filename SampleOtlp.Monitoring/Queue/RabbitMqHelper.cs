using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace SampleOtlp.Monitoring.Queue;

public class RabbitMqHelper
{
    public const string DefaultExchangeName = "";
    public const string TestQueueName = "TestOTEL";

    private static readonly ConnectionFactory ConnectionFactory;

    static RabbitMqHelper()
    {
        ConnectionFactory = new ConnectionFactory()
        {
            HostName = "localhost",
            UserName = "admin",
            Password = "Admin@123",
            Port = 5672,
            RequestedConnectionTimeout = (int)TimeSpan.FromMilliseconds(3000).TotalSeconds,
        };
    }

    public static IConnection CreateConnection()
    {
        return ConnectionFactory.CreateConnection();
    }

    public static IModel CreateModelAndDeclareTestQueue(IConnection connection)
    {
        var channel = connection.CreateModel();

        channel.QueueDeclare(
            queue: TestQueueName,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        return channel;
    }

    public static void StartConsumer(IModel channel, Action<BasicDeliverEventArgs> processMessage)
    {
        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += (bc, ea) => processMessage(ea);

        channel.BasicConsume(queue: TestQueueName, autoAck: true, consumer: consumer);
    }

    public static void AddMessagingTags(Activity activity)
    {
        // These tags are added demonstrating the semantic conventions of the OpenTelemetry messaging specification
        // See:
        //   * https://github.com/open-telemetry/semantic-conventions/blob/main/docs/messaging/messaging-spans.md#messaging-attributes
        //   * https://github.com/open-telemetry/semantic-conventions/blob/main/docs/messaging/rabbitmq.md
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination_kind", "queue");
        activity?.SetTag("messaging.destination", DefaultExchangeName);
        activity?.SetTag("messaging.rabbitmq.queue", TestQueueName);
        activity?.SetTag("messaging.rabbitmq.routing_key", "");
    }
}