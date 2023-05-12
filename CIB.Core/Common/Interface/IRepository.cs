using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace CIB.Core.Common.Interface
{
    public interface IRepository<T> where T : class
    {
        T GetByIdAsync(Guid id);
        Task<IReadOnlyList<T>> ListAllAsync();
        Task<IReadOnlyList<T>> GetPagedReponseAsync(int page, int size);
        T Find(Expression<Func<T, bool>> predicate);
        void AddRange(IEnumerable<T> T);
        void RemoveRange(IEnumerable<T> T);
        void Add(T entity);
        void Update(T entity);
    }
}