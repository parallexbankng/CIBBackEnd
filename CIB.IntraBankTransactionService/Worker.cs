using System.Transactions;
using CIB.IntraBankTransactionService.Jobs;
using CIB.IntraBankTransactionService.Utils;
using Newtonsoft.Json;

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
		try
		{
			while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
			{
				await _postIntraBankTransaction.Run();
			}
		}
		catch (Exception ex)
		{
			_logger.LogError("SERVER ERROR {0}, {1}", JsonConvert.SerializeObject(ex.Message), JsonConvert.SerializeObject(ex.StackTrace));
		}
	}
}
