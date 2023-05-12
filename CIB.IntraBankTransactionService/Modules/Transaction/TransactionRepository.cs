
using CIB.IntraBankTransactionService.Entities;
using CIB.IntraBankTransactionService.Modules.Common.Repository;

namespace CIB.IntraBankTransactionService.Modules.Transaction;

public class TransactionRepository : Repository<TblTransaction>,ITransactionRepository
{ public TransactionRepository(ParallexCIBContext context) : base(context)
  {
  }
  public ParallexCIBContext Context { get { return _context as ParallexCIBContext; }} 
}
