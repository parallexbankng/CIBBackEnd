using CIB.IntraBankTransactionService.Modules.BulkCreditLog;
using CIB.IntraBankTransactionService.Modules.Transaction;
using CIB.IntraBankTransactionService.Modules.BulkPaymentLog;
using CIB.IntraBankTransactionService.Entities;
using CIB.IntraBankTransactionService.Modules.Common.Interface;

namespace CIB.IntraBankTransactionService.Modules.Common.Repository;

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
