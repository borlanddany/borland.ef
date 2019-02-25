using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Borland.EF
{
    public class BorlandEntityType : EntityType
    {
        private readonly SortedDictionary<string, Navigation> _navigations
            = new SortedDictionary<string, Navigation>(StringComparer.Ordinal);
        private readonly DbContext _context;

        public BorlandEntityType(
            [NotNull] Type clrType, [NotNull] Model model,
            ConfigurationSource configurationSource,
            DbContext context)
            : base(clrType, model, configurationSource)
        {
            _context = context;
        }

        public BorlandEntityType(
            [NotNull] string name, [NotNull] Model model,
            [NotNull] string definingNavigationName,
            [NotNull] EntityType definingEntityType,
            ConfigurationSource configurationSource,
            DbContext context)
            : base(name, model, definingNavigationName,
                  definingEntityType, configurationSource)
        {
            _context = context;
        }

        public BorlandEntityType(
            [NotNull] Type clrType, [NotNull] Model model,
            [NotNull] string definingNavigationName,
            [NotNull] EntityType definingEntityType,
            ConfigurationSource configurationSource,
            DbContext context)
            : base(clrType, model, definingNavigationName,
                  definingEntityType, configurationSource)
        {
            _context = context;
        }

        public override Navigation AddNavigation(
            [NotNull] string name,
            [NotNull] ForeignKey foreignKey,
            bool pointsToPrincipal)
        {
            return AddNavigation(new PropertyIdentity(name), foreignKey, pointsToPrincipal);
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public override Navigation AddNavigation(
            [NotNull] MemberInfo navigationProperty,
            [NotNull] ForeignKey foreignKey,
            bool pointsToPrincipal)
        {
            return AddNavigation(new PropertyIdentity(navigationProperty), foreignKey, pointsToPrincipal);
        }

        private Navigation AddNavigation(PropertyIdentity propertyIdentity, ForeignKey foreignKey, bool pointsToPrincipal)
        {
            var name = propertyIdentity.Name;
            var duplicateNavigation = FindNavigationsInHierarchy(name).FirstOrDefault();
            if (duplicateNavigation != null)
            {
                if (duplicateNavigation.ForeignKey != foreignKey)
                {
                    throw new InvalidOperationException(
                        CoreStrings.NavigationForWrongForeignKey(
                            duplicateNavigation.Name,
                            duplicateNavigation.DeclaringEntityType.DisplayName(),
                            Property.Format(foreignKey.Properties),
                            Property.Format(duplicateNavigation.ForeignKey.Properties)));
                }

                throw new InvalidOperationException(
                    CoreStrings.ConflictingPropertyOrNavigation(name, this.DisplayName(), duplicateNavigation.DeclaringEntityType.DisplayName()));
            }

            var duplicateProperty = FindPropertiesInHierarchy(name).Cast<PropertyBase>()
                .Concat(FindServicePropertiesInHierarchy(name)).FirstOrDefault();
            if (duplicateProperty != null)
            {
                throw new InvalidOperationException(
                    CoreStrings.ConflictingPropertyOrNavigation(name, this.DisplayName(),
                        duplicateProperty.DeclaringType.DisplayName()));
            }

            Debug.Assert(
                !GetNavigations().Any(n => n.ForeignKey == foreignKey && n.IsDependentToPrincipal() == pointsToPrincipal),
                "There is another navigation corresponding to the same foreign key and pointing in the same direction.");

            Debug.Assert(
                (pointsToPrincipal ? foreignKey.DeclaringEntityType : foreignKey.PrincipalEntityType) == this,
                "EntityType mismatch");

            var navigationProperty = propertyIdentity.Property;
            if (ClrType != null)
            {
                Navigation.IsCompatible(
                    propertyIdentity.Name,
                    navigationProperty,
                    this,
                    pointsToPrincipal ? foreignKey.PrincipalEntityType : foreignKey.DeclaringEntityType,
                    !pointsToPrincipal && !foreignKey.IsUnique,
                    shouldThrow: true);
            }

            var navigation = new BorlandNavigation(name, propertyIdentity.Property as PropertyInfo, propertyIdentity.Property as FieldInfo, foreignKey, _context);

            _navigations.Add(name, navigation);

            PropertyMetadataChanged();

            return navigation;
        }

        public override Navigation FindDeclaredNavigation([NotNull] string name)
            => _navigations.TryGetValue(name, out var navigation)
                ? navigation
                : null;

        public override IEnumerable<Navigation> GetDeclaredNavigations() => _navigations.Values;

        public override Navigation RemoveNavigation([NotNull] string name)
        {
            var navigation = FindDeclaredNavigation(name);
            if (navigation == null)
            {
                return null;
            }

            _navigations.Remove(name);

            PropertyMetadataChanged();

            return navigation;
        }

        public override IEnumerable<Navigation> GetNavigations()
            => BaseType?.GetNavigations().Concat(_navigations.Values) ?? _navigations.Values;
    }
}
