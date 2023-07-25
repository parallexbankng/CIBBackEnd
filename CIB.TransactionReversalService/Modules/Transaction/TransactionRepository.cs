
using CIB.TransactionReversalService.Entities;
using CIB.TransactionReversalService.Modules.Common.Repository;

namespace CIB.TransactionReversalService.Modules.Transaction;

public class TransactionRepository : Repository<TblTransaction>,ITransactionRepository
{ public TransactionRepository(ParallexCIBContext context) : base(context)
  {
  }
  
  public ParallexCIBContext Context { get { return _context as ParallexCIBContext; }}
}
