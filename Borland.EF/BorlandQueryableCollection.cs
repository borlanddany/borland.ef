using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Borland.EF
{
    public class BorlandQueryableCollection<TEntity> : IQueryable<TEntity>, ICollection<TEntity> where TEntity : class
    {
        private readonly IQueryable<TEntity> _queryable;
        private readonly DbContext _context;

        public BorlandQueryableCollection(IQueryable<TEntity> queryable, DbContext context)
        {
            _queryable = queryable;
            _context = context;
        }

        public Type ElementType => _queryable.ElementType;

        public Expression Expression => _queryable.Expression;

        public IQueryProvider Provider => _queryable.Provider;

        public int Count => _queryable.Count();

        public bool IsReadOnly => false;

        public void Add(TEntity item)
        {
            var entry = _context.Add(item);
            //_context.SaveChanges();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(TEntity item) => _queryable.Contains(item);

        public void CopyTo(TEntity[] array, int arrayIndex)
        {
            _queryable.ToArray().CopyTo(array, arrayIndex);
        }

        public IEnumerator<TEntity> GetEnumerator() => _queryable.GetEnumerator();

        public bool Remove(TEntity item)
        {
            if (!_context.Set<TEntity>().Contains(item))
            {
                _context.Remove(item);
                return true;
            }
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator() => _queryable.GetEnumerator();
    }
}
