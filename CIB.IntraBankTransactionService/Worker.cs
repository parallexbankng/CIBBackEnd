using CIB.IntraBankTransactionService.Jobs;

namespace CIB.IntraBankTransactionService;

public class Worker : BackgroundService
{
  private readonly ILogger<Worker> _logger;
  private readonly IIntraBankJob _postIntraBankTransaction;
  private readonly PeriodicTimer _timer = new(TimeSpan.FromMilliseconds(8000));
  
  public Worker(ILogger<Worker> logger, IIntraBankJob postIntraBankTransaction)
  {
    _logger = logger;
    _postIntraBankTransaction = postIntraBankTransaction;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
    {
      _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
      await _postIntraBankTransaction.Run();
    }
  }
}
