using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Borland.EF
{
    public class BorlandClrICollectionAccessor<TEntity, TCollection, TElement> : IClrCollectionAccessor
    {
        private readonly Func<TEntity, ICollection<TElement>> _getCollectionFunc;
        private readonly Func<ICollection<TElement>> _createCollectionFunc;

        public BorlandClrICollectionAccessor(
            Func<TEntity, ICollection<TElement>> getCollectionFunc,
            Func<ICollection<TElement>> createCollectionFunc)
        {
            _getCollectionFunc = getCollectionFunc;
            _createCollectionFunc = createCollectionFunc;
        }

        public Type CollectionType => typeof(TCollection);

        public bool Add(object instance, object value)
        {
            var collection = (ICollection<TElement>)GetOrCreate(instance);
            var element = (TElement)value;

            if (!collection.Contains(element))
            {
                collection.Add(element);
                return true;
            }
            return false;
        }

        public void AddRange(object instance, IEnumerable<object> values)
        {
            var collection = (ICollection<TElement>)GetOrCreate(instance);

            foreach (TElement value in values)
            {
                if (!collection.Contains(value))
                {
                    collection.Add(value);
                }
            }
        }

        public bool Contains(object instance, object value) => ((ICollection<TElement>)GetOrCreate(instance)).Contains((TElement)value);

        public object Create() => _createCollectionFunc();

        public object Create(IEnumerable<object> values)
        {
            var collection = (ICollection<TElement>)Create();
            foreach (TElement value in values)
            {
                collection.Add(value);
            }
            return collection;
        }

        public object GetOrCreate(object instance) => _getCollectionFunc((TEntity)instance);

        public void Remove(object instance, object value) => ((ICollection<TElement>)GetOrCreate(instance)).Remove((TElement)value);
    }
}
