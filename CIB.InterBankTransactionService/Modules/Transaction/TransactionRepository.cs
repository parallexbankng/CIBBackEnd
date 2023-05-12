
using CIB.InterBankTransactionService.Entities;
using CIB.InterBankTransactionService.Modules.Common.Repository;

namespace CIB.InterBankTransactionService.Modules.Transaction;

public class TransactionRepository : Repository<TblTransaction>,ITransactionRepository
{ public TransactionRepository(ParallexCIBContext context) : base(context)
  {
  }
  public ParallexCIBContext Context { get { return _context as ParallexCIBContext; }} 
}
