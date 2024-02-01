using CIB.InterBankTransactionService.Jobs;
using CIB.InterBankTransactionService.Utils;

namespace CIB.InterBankTransactionService;

public class Worker : BackgroundService
{
	private readonly ILogger<Worker> _logger;
	private readonly IInterBankJob _postInterBankTransaction;
	private readonly PeriodicTimer _timer = new(TimeSpan.FromMilliseconds(8000));

	public Worker(ILogger<Worker> logger, IInterBankJob postInterBankTransaction)
	{
		_logger = logger;
		this._postInterBankTransaction = postInterBankTransaction;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
		{
			await _postInterBankTransaction.Run();
		}
	}
}
