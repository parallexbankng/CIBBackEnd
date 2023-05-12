using CIB.InterBankTransactionService.Entities;
using CIB.InterBankTransactionService.Modules.BulkCreditLog;
using CIB.InterBankTransactionService.Modules.Transaction;
using CIB.InterBankTransactionService.Modules.BulkPaymentLog;
using CIB.InterBankTransactionService.Modules.Common.Interface;

namespace CIB.InterBankTransactionService.Modules.Common.Repository;

public class UnitOfWork : IUnitOfWork
{
  private readonly ParallexCIBContext _dbContext;
  public UnitOfWork(ParallexCIBContext context)
  {
    _dbContext = context;
    BulkCreditLogRepo = new BulkCreditLogRepository(_dbContext);
    BulkPaymentLogRepo = new BulkPaymentLogRepository(_dbContext);
    TransactionRepo = new TransactionRepository(_dbContext);
  }
  public IBulkCreditLogRepository BulkCreditLogRepo { get; protected set; }
  public IBulkPaymentLogRepository BulkPaymentLogRepo { get; protected set; }
  public ITransactionRepository TransactionRepo { get; protected set; }

  public int Complete()
  {
    return _dbContext.SaveChanges();
  }

  public void Dispose()
  {
    _dbContext.Dispose();
  }
}
