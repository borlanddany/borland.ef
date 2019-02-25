using System;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Borland.EF
{
    public class BorlandNavigation : Navigation
    {
        private readonly DbContext _context;
        private IClrCollectionAccessor _collectionAccessor;

        public BorlandNavigation(
            [NotNull] string name,
            [CanBeNull] PropertyInfo propertyInfo,
            [CanBeNull] FieldInfo fieldInfo,
            [NotNull] ForeignKey foreignKey,
            [NotNull] DbContext context)
            : base(name, propertyInfo,
                  fieldInfo, foreignKey)
        {
            _context = context;
        }

        public override IClrCollectionAccessor CollectionAccessor
            => NonCapturingLazyInitializer.EnsureInitialized(ref _collectionAccessor, this, n =>
                !n.IsCollection() || n.IsShadowProperty
                    ? null
                    : new BorlandClrCollectionAccessorFactory(_context).Create(n));
    }
}
