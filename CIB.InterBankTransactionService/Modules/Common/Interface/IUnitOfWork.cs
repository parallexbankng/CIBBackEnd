
using CIB.InterBankTransactionService.Modules.BulkCreditLog;
using CIB.InterBankTransactionService.Modules.BulkPaymentLog;
using CIB.InterBankTransactionService.Modules.Transaction;

namespace CIB.InterBankTransactionService.Modules.Common.Interface;

public interface IUnitOfWork : IDisposable
{
  IBulkPaymentLogRepository BulkPaymentLogRepo { get; }
  IBulkCreditLogRepository BulkCreditLogRepo { get; }
  ITransactionRepository TransactionRepo { get; }
  int Complete();
  new void Dispose();
}
