
using CIB.InterBankTransactionService.Entities;
using CIB.InterBankTransactionService.Modules.Common.Interface;

namespace CIB.InterBankTransactionService.Modules.Common.Repository;

   public class Repository<T> : IRepository<T> where T : class
  {
    protected readonly ParallexCIBContext _context;
      protected Repository(ParallexCIBContext context)
      {
        _context = context;
      }

      public void Add(T entity)
      {
        _context.Set<T>().Add(entity);
      }

      public void AddRange(IEnumerable<T> T)
      {
        _context.Set<T>().AddRange(T);
      }

      public async Task<T> GetByIdAsync(Guid id)
      {
          return await _context.Set<T>().FindAsync(id);
      }

      public Task<List<T>> ListAllAsync()
      {
          return Task.FromResult(_context.Set<T>().ToList());
      }

      public void RemoveRange(IEnumerable<T> T)
      {
        _context.Set<T>().RemoveRange(T);
      }
  }
