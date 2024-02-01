using CIB.TransactionReversalService.Jobs;

namespace CIB.TransactionReversalService;

public class Worker : BackgroundService
{
  private readonly ILogger<Worker> _logger;
  private readonly ITransactionReversal _reversal;
  private readonly PeriodicTimer _timer = new(TimeSpan.FromMilliseconds(6000));

  public Worker(ILogger<Worker> logger,ITransactionReversal reversal)
  {
    _logger = logger;
    _reversal = reversal;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    while (await _timer.WaitForNextTickAsync(stoppingToken)  && !stoppingToken.IsCancellationRequested)
    {
      await _reversal.Run();
    }
  }
}
