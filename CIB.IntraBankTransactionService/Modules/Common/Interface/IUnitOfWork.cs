
using CIB.IntraBankTransactionService.Modules.BulkCreditLog;
using CIB.IntraBankTransactionService.Modules.BulkPaymentLog;
using CIB.IntraBankTransactionService.Modules.Transaction;

namespace CIB.IntraBankTransactionService.Modules.Common.Interface
{
  public interface IUnitOfWork : IDisposable
  {
    IBulkPaymentLogRepository BulkPaymentLogRepo { get; }
    IBulkCreditLogRepository BulkCreditLogRepo { get; }
    ITransactionRepository TransactionRepo { get; }
    int Complete();
    new void Dispose();
  }
}
