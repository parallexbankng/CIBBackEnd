using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;

namespace CIB.Core.Modules.OnLending.Transaction
{
  public class OnlendingTransactionRepository : Repository<TblOnlendingTransaction>, IOnlendingTransactionRepository
  {
    public OnlendingTransactionRepository(ParallexCIBContext context) : base(context)
    {
    }

    public ParallexCIBContext context
    {
      get { return _context as ParallexCIBContext; }
    }

    void IRepository<TblOnlendingExtensionHistory>.Add(TblOnlendingExtensionHistory entity)
    {
      throw new NotImplementedException();
    }

    void IRepository<TblOnlendingExtensionHistory>.AddRange(IEnumerable<TblOnlendingExtensionHistory> T)
    {
      throw new NotImplementedException();
    }

    TblOnlendingExtensionHistory IRepository<TblOnlendingExtensionHistory>.Find(Expression<Func<TblOnlendingExtensionHistory, bool>> predicate)
    {
      throw new NotImplementedException();
    }

    TblOnlendingExtensionHistory IRepository<TblOnlendingExtensionHistory>.GetByIdAsync(Guid id)
    {
      throw new NotImplementedException();
    }

    Task<IReadOnlyList<TblOnlendingExtensionHistory>> IRepository<TblOnlendingExtensionHistory>.GetPagedReponseAsync(int page, int size)
    {
      throw new NotImplementedException();
    }

    Task<IReadOnlyList<TblOnlendingExtensionHistory>> IRepository<TblOnlendingExtensionHistory>.ListAllAsync()
    {
      throw new NotImplementedException();
    }

    void IRepository<TblOnlendingExtensionHistory>.Remove(TblOnlendingExtensionHistory entity)
    {
      throw new NotImplementedException();
    }

    void IRepository<TblOnlendingExtensionHistory>.RemoveRange(IEnumerable<TblOnlendingExtensionHistory> T)
    {
      throw new NotImplementedException();
    }

    void IRepository<TblOnlendingExtensionHistory>.Update(TblOnlendingExtensionHistory entity)
    {
      throw new NotImplementedException();
    }
  }
}