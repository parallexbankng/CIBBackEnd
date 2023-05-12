
using CIB.TransactionReversalService.Entities;
using CIB.TransactionReversalService.Modules.Common.Interface;

namespace CIB.TransactionReversalService.Modules.Common.Repository;

   public class Repository<T> : IRepository<T> where T : class
  {
    protected readonly ParallexCIBContext Context;
    protected Repository(ParallexCIBContext context)
    {
        Context = context;
    }

    public void Add(T entity)
    {
        Context.Set<T>().Add(entity);
    }

    public void AddRange(IEnumerable<T> T)
    {
        Context.Set<T>().AddRange(T);
    }

    public async Task<T> GetByIdAsync(Guid id)
    {
        return await Context.Set<T>().FindAsync(id);
    }

    public Task<List<T>> ListAllAsync()
    {
        return Task.FromResult(Context.Set<T>().ToList());
    }

    public void RemoveRange(IEnumerable<T> T)
    {
        Context.Set<T>().RemoveRange(T);
    }
  }
