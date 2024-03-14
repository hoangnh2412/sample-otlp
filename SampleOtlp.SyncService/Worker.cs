using SampleOtlp.Monitoring.Queue;

namespace SampleOtlp.SyncService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    private readonly MessageReceiver _messageReceiver;

    public Worker(
        MessageReceiver messageReceiver,
        ILogger<Worker> logger)
    {
        _messageReceiver = messageReceiver;
        _logger = logger;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        return base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        this._messageReceiver.StartConsumer2();

        await Task.CompletedTask.ConfigureAwait(false);
    }
}
