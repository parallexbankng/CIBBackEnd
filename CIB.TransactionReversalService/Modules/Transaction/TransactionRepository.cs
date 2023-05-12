
using CIB.TransactionReversalService.Entities;
using CIB.TransactionReversalService.Modules.Common.Repository;

namespace CIB.TransactionReversalService.Modules.Transaction;

public class TransactionRepository : Repository<TblTransaction>,ITransactionRepository
{ public TransactionRepository(ParallexCIBContext context) : base(context)
  {
  }
  public new ParallexCIBContext Context { get { return base.Context as ParallexCIBContext; }} 
}
