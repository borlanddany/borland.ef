using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Borland.EF
{
    public class BorlandClrCollectionAccessorFactory : ClrCollectionAccessorFactory
    {
        private static readonly MethodInfo _genericCreate
            = typeof(BorlandClrCollectionAccessorFactory).GetTypeInfo().GetDeclaredMethod(nameof(CreateGeneric));

        private readonly DbContext _context;

        public BorlandClrCollectionAccessorFactory(DbContext context)
        {
            _context = context;
        }

        public override IClrCollectionAccessor Create([NotNull] INavigation navigation)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (navigation is IClrCollectionAccessor accessor)
            {
                return accessor;
            }

            var property = navigation.GetIdentifyingMemberInfo();
            var propertyType = property.GetMemberType();
            var elementType = propertyType.TryGetElementType(typeof(IEnumerable<>));

            if (elementType == null)
            {
                throw new InvalidOperationException(
                    CoreStrings.NavigationBadType(
                        navigation.Name,
                        navigation.DeclaringEntityType.DisplayName(),
                        propertyType.ShortDisplayName(),
                        navigation.GetTargetType().DisplayName()));
            }

            if (propertyType.IsArray)
            {
                throw new InvalidOperationException(
                    CoreStrings.NavigationArray(
                        navigation.Name,
                        navigation.DeclaringEntityType.DisplayName(),
                        propertyType.ShortDisplayName()));
            }

            var boundMethod = _genericCreate.MakeGenericMethod(
                property.DeclaringType, propertyType, elementType);

            var memberInfo = navigation.GetMemberInfo(forConstruction: false, forSet: false);

            return (IClrCollectionAccessor)boundMethod.Invoke(this, new object[] { navigation, memberInfo });
        }

        [UsedImplicitly]
        private IClrCollectionAccessor CreateGeneric<TEntity, TCollection, TElement>(INavigation navigation, MemberInfo memberInfo)
            where TEntity : class
            where TElement : class
            where TCollection : class, IEnumerable<TElement>
        {
            var entityParameter = Expression.Parameter(typeof(TEntity), "entity");
            var valueParameter = Expression.Parameter(typeof(TCollection), "collection");

            var getterDelegate = Expression.Lambda<Func<TEntity, TCollection>>(
                Expression.MakeMemberAccess(
                    entityParameter,
                    memberInfo),
                entityParameter).Compile();

            Action<TEntity, TCollection> setterDelegate = null;
            Func<TEntity, Action<TEntity, TCollection>, TCollection> createAndSetDelegate = null;
            Func<TCollection> createDelegate = null;

            var setterMemberInfo = navigation.GetMemberInfo(forConstruction: false, forSet: true);
            if (setterMemberInfo != null)
            {
                setterDelegate = Expression.Lambda<Action<TEntity, TCollection>>(
                    Expression.Assign(
                        Expression.MakeMemberAccess(
                            entityParameter,
                            setterMemberInfo),
                        Expression.Convert(
                            valueParameter,
                            setterMemberInfo.GetMemberType())),
                    entityParameter,
                    valueParameter).Compile();
            }

            if (setterDelegate != null)
            {
                var concreteType = new CollectionTypeFactory().TryFindTypeToInstantiate(typeof(TEntity), typeof(TCollection));

                if (concreteType != null)
                {
                    createAndSetDelegate = new Func<TEntity, Action<TEntity, TCollection>, TCollection>((entity, setter) =>
                    {
                        var collection = (TCollection)Activator.CreateInstance(concreteType);
                        setterDelegate(entity, collection);
                        return collection;
                    });

                    createDelegate = new Func<TCollection>(() => (TCollection)Activator.CreateInstance(concreteType));
                }
                else
                {
                    if (typeof(TCollection).IsAssignableFrom(typeof(IQueryable<TElement>)))
                    {
                        var primaryKey = navigation.ForeignKey.PrincipalKey.Properties[0].Name;
                        var parameter = Expression.Parameter(typeof(TElement), "x");
                        var primaryKeyProperty = typeof(TEntity).GetProperty(primaryKey, BindingFlags.Public | BindingFlags.Instance);
                        var navigationProperty = typeof(TElement).GetProperties(BindingFlags.Public | BindingFlags.Instance).Single(x => x.PropertyType == typeof(TEntity));
                        var foreignValue = Expression.Property(Expression.Property(parameter, navigationProperty), primaryKeyProperty);
                        var includeParameter = Expression.Parameter(typeof(TElement), "x");
                        var includeExpression = Expression.Lambda<Func<TElement, TEntity>>(Expression.Property(includeParameter, navigationProperty), includeParameter);
                        return new BorlandClrICollectionAccessor<TEntity, TCollection, TElement>(
                            (entity) =>
                            {
                                var primaryValue = Expression.Property(Expression.Constant(entity), primaryKeyProperty);
                                var expression = Expression.Lambda<Func<TElement, bool>>(Expression.Equal(foreignValue, primaryValue), parameter);
                                return new BorlandQueryableCollection<TElement>(_context.Set<TElement>().Include(includeExpression).Where(expression), _context);
                            },
                            () => new BorlandQueryableCollection<TElement>(_context.Set<TElement>(), _context));
                    }
                }
            }

            return new ClrICollectionAccessor<TEntity, TCollection, TElement>(
                navigation.Name, getterDelegate, setterDelegate, createAndSetDelegate, createDelegate);
        }
    }
}
