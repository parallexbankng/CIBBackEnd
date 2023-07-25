using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CIB.Core.Common.Repository
{
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

        public T Find(Expression<Func<T, bool>> predicate)
        {
            return _context.Set<T>().Where(predicate).First();
        }

        public T GetByIdAsync(Guid id)
        {
            return _context.Set<T>().Find(id);
        }

        public async virtual Task<IReadOnlyList<T>> GetPagedReponseAsync(int page, int size)
        {
            return await _context.Set<T>().Skip((page - 1) * size).Take(size).AsNoTracking().ToListAsync();
        }

        public async Task<IReadOnlyList<T>> ListAllAsync()
        {
            return await _context.Set<T>().ToListAsync();
        }

        public void RemoveRange(IEnumerable<T> T)
        {
            _context.Set<T>().RemoveRange(T);
        }

        public void Update(T entity)
        {
            _context.Update(entity);
        }

       public  void Remove(T entity)
        {
            _context.Set<T>().Remove(entity);
        }
  }
}