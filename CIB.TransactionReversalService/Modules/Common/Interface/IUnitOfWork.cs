
using CIB.TransactionReversalService.Modules.BulkCreditLog;
using CIB.TransactionReversalService.Modules.BulkPaymentLog;
using CIB.TransactionReversalService.Modules.Transaction;

namespace CIB.TransactionReversalService.Modules.Common.Interface;

public interface IUnitOfWork : IDisposable
{
  IBulkPaymentLogRepository BulkPaymentLogRepo { get; }
  IBulkCreditLogRepository BulkCreditLogRepo { get; }
  ITransactionRepository TransactionRepo { get; }
  int Complete();
  new void Dispose();
}
