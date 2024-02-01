
namespace CIB.TransactionReversalService.Modules.Common.Interface;
  public interface IRepository<T> where T : class
  {
    Task<T> GetByIdAsync(Guid id);
    Task<List<T>> ListAllAsync();
    void AddRange(IEnumerable<T> T);
    void Add(T entity);
  }
