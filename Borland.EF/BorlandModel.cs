using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Borland.EF
{
    public class BorlandModel : Model
    {
        private readonly DbContext _context;

        private readonly SortedDictionary<string, EntityType> _entityTypes;

        private readonly SortedDictionary<string, SortedSet<EntityType>> _entityTypesWithDefiningNavigation;

        public override IEnumerable<EntityType> GetEntityTypes()
            => _entityTypes.Values.Concat(_entityTypesWithDefiningNavigation.Values.SelectMany(e => e));

        public BorlandModel([NotNull] ConventionSet conventions, [NotNull] DbContext context)
            : base(conventions)
        {
            _context = context;
            _entityTypes = new SortedDictionary<string, EntityType>();
            _entityTypesWithDefiningNavigation = new SortedDictionary<string, SortedSet<EntityType>>();
        }

        public override EntityType AddEntityType(
            [NotNull] Type type,
            // ReSharper disable once MethodOverloadWithOptionalParameter
            ConfigurationSource configurationSource = ConfigurationSource.Explicit)
        {
            var entityType = new BorlandEntityType(type, this, configurationSource, _context);

            return AddEntityType(entityType);
        }

        public override EntityType AddQueryType(
            [NotNull] Type type,
            // ReSharper disable once MethodOverloadWithOptionalParameter
            ConfigurationSource configurationSource = ConfigurationSource.Explicit)
        {
            var queryType = new BorlandEntityType(type, this, configurationSource, _context)
            {
                IsQueryType = true
            };

            return AddEntityType(queryType);
        }

        public override EntityType AddEntityType(
            [NotNull] string name,
            [NotNull] string definingNavigationName,
            [NotNull] EntityType definingEntityType,
            ConfigurationSource configurationSource = ConfigurationSource.Explicit)
        {
            var entityType = new BorlandEntityType(name, this, definingNavigationName, definingEntityType, configurationSource, _context);

            return AddEntityType(entityType);
        }

        public override EntityType AddEntityType(
            [NotNull] Type type,
            [NotNull] string definingNavigationName,
            [NotNull] EntityType definingEntityType,
            ConfigurationSource configurationSource = ConfigurationSource.Explicit)
        {
            var entityType = new BorlandEntityType(type, this, definingNavigationName, definingEntityType, configurationSource, _context);

            return AddEntityType(entityType);
        }

        private EntityType AddEntityType(EntityType entityType)
        {
            var entityTypeName = entityType.Name;
            if (entityType.HasDefiningNavigation())
            {
                if (_entityTypes.ContainsKey(entityTypeName))
                {
                    throw new InvalidOperationException(CoreStrings.ClashingNonWeakEntityType(entityType.DisplayName()));
                }

                if (!_entityTypesWithDefiningNavigation.TryGetValue(entityTypeName, out var entityTypesWithSameType))
                {
                    entityTypesWithSameType = new SortedSet<EntityType>(EntityTypePathComparer.Instance);
                    _entityTypesWithDefiningNavigation[entityTypeName] = entityTypesWithSameType;
                }

                var added = entityTypesWithSameType.Add(entityType);
                Debug.Assert(added);
            }
            else
            {
                if (_entityTypesWithDefiningNavigation.ContainsKey(entityTypeName))
                {
                    throw new InvalidOperationException(CoreStrings.ClashingWeakEntityType(entityType.DisplayName()));
                }

                if (AppContext.TryGetSwitch("Microsoft.EntityFrameworkCore.Issue12119", out var isEnabled)
                    && isEnabled)
                {
                    var previousLength = _entityTypes.Count;
                    _entityTypes[entityTypeName] = entityType;
                    if (previousLength == _entityTypes.Count)
                    {
                        throw new InvalidOperationException(CoreStrings.DuplicateEntityType(entityType.DisplayName()));
                    }
                }
                else
                {
                    if (_entityTypes.TryGetValue(entityTypeName, out var clashingEntityType))
                    {
                        if (clashingEntityType.IsQueryType)
                        {
                            if (entityType.IsQueryType)
                            {
                                throw new InvalidOperationException(CoreStrings.DuplicateQueryType(entityType.DisplayName()));
                            }
                            throw new InvalidOperationException(CoreStrings.CannotAccessQueryAsEntity(entityType.DisplayName()));
                        }

                        if (entityType.IsQueryType)
                        {
                            throw new InvalidOperationException(CoreStrings.CannotAccessEntityAsQuery(entityType.DisplayName()));
                        }
                        throw new InvalidOperationException(CoreStrings.DuplicateEntityType(entityType.DisplayName()));
                    }

                    _entityTypes.Add(entityTypeName, entityType);
                }
            }

            return ConventionDispatcher.OnEntityTypeAdded(entityType.Builder)?.Metadata;
        }

        public override EntityType FindEntityType([NotNull] string name)
            => _entityTypes.TryGetValue(name, out var entityType)
                ? entityType
                : null;

        private static void AssertCanRemove(EntityType entityType)
        {
            var foreignKey = entityType.GetDeclaredForeignKeys().FirstOrDefault(fk => fk.PrincipalEntityType != entityType);
            if (foreignKey != null)
            {
                throw new InvalidOperationException(
                    CoreStrings.EntityTypeInUseByForeignKey(
                        entityType.DisplayName(),
                        foreignKey.PrincipalEntityType.DisplayName(),
                        Property.Format(foreignKey.Properties)));
            }

            var referencingForeignKey = entityType.GetDeclaredReferencingForeignKeys().FirstOrDefault();
            if (referencingForeignKey != null)
            {
                throw new InvalidOperationException(
                    CoreStrings.EntityTypeInUseByReferencingForeignKey(
                        entityType.DisplayName(),
                        Property.Format(referencingForeignKey.Properties),
                        referencingForeignKey.DeclaringEntityType.DisplayName()));
            }

            var derivedEntityType = entityType.GetDirectlyDerivedTypes().FirstOrDefault();
            if (derivedEntityType != null)
            {
                throw new InvalidOperationException(
                    CoreStrings.EntityTypeInUseByDerived(
                        entityType.DisplayName(),
                        derivedEntityType.DisplayName()));
            }
        }

        public override EntityType RemoveEntityType([CanBeNull] EntityType entityType)
        {
            if (entityType?.Builder == null)
            {
                return null;
            }

            AssertCanRemove(entityType);

            var entityTypeName = entityType.Name;
            if (entityType.HasDefiningNavigation())
            {
                if (!_entityTypesWithDefiningNavigation.TryGetValue(entityTypeName, out var entityTypesWithSameType))
                {
                    return null;
                }

                var removed = entityTypesWithSameType.Remove(entityType);
                Debug.Assert(removed);

                if (entityTypesWithSameType.Count == 0)
                {
                    _entityTypesWithDefiningNavigation.Remove(entityTypeName);
                }
            }
            else
            {
                var removed = _entityTypes.Remove(entityTypeName);
                Debug.Assert(removed);
            }

            entityType.OnTypeRemoved();

            return entityType;
        }

        public override bool HasEntityTypeWithDefiningNavigation([NotNull] Type clrType)
            => _entityTypesWithDefiningNavigation.ContainsKey(GetDisplayName(clrType));

        public override bool HasEntityTypeWithDefiningNavigation([NotNull] string name)
            => _entityTypesWithDefiningNavigation.ContainsKey(name);

        public override EntityType FindEntityType(
            [NotNull] string name,
            [NotNull] string definingNavigationName,
            [NotNull] string definingEntityTypeName)
        {
            if (!_entityTypesWithDefiningNavigation.TryGetValue(name, out var entityTypesWithSameType))
            {
                return null;
            }

            return entityTypesWithSameType
                .FirstOrDefault(e => e.DefiningNavigationName == definingNavigationName
                                     && e.DefiningEntityType.Name == definingEntityTypeName);
        }

        public override EntityType FindEntityType(
            [NotNull] string name,
            [NotNull] string definingNavigationName,
            [NotNull] EntityType definingEntityType)
        {
            if (!_entityTypesWithDefiningNavigation.TryGetValue(name, out var entityTypesWithSameType))
            {
                return null;
            }

            return entityTypesWithSameType
                .FirstOrDefault(e => e.DefiningNavigationName == definingNavigationName && e.DefiningEntityType == definingEntityType);
        }

        public override IReadOnlyCollection<EntityType> GetEntityTypes([NotNull] string name)
        {
            if (_entityTypesWithDefiningNavigation.TryGetValue(name, out var entityTypesWithSameType))
            {
                return entityTypesWithSameType;
            }

            var entityType = FindEntityType(name);
            return entityType == null ? Array.Empty<EntityType>() : new[] { entityType };
        }
    }
}
