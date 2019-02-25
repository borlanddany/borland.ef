using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Borland.EF
{
    public class BorlandModelBuilder : ModelBuilder, IInfrastructure<InternalModelBuilder>
    {
        public BorlandModelBuilder([NotNull] ConventionSet conventions, [NotNull] DbContext context)
            : base(conventions)
        {
            Instance = new InternalModelBuilder(new BorlandModel(conventions, context));
        }

        public InternalModelBuilder Instance { get; }

        public override IMutableModel Model => Instance.Metadata;

        public override ModelBuilder HasAnnotation([NotNull] string annotation, [NotNull] object value)
        {
            Instance.HasAnnotation(annotation, value, ConfigurationSource.Explicit);

            return this;
        }

        public override EntityTypeBuilder<TEntity> Entity<TEntity>()
            => new EntityTypeBuilder<TEntity>(Instance.Entity(typeof(TEntity), ConfigurationSource.Explicit, throwOnQuery: true));

        public override EntityTypeBuilder Entity([NotNull] Type type)
        {
            return new EntityTypeBuilder(Instance.Entity(type, ConfigurationSource.Explicit, throwOnQuery: true));
        }

        public override EntityTypeBuilder Entity([NotNull] string name)
        {
            return new EntityTypeBuilder(Instance.Entity(name, ConfigurationSource.Explicit, throwOnQuery: true));
        }

        public override ModelBuilder Entity<TEntity>([NotNull] Action<EntityTypeBuilder<TEntity>> buildAction)
        {
            buildAction(Entity<TEntity>());

            return this;
        }

        public override ModelBuilder Entity([NotNull] Type type, [NotNull] Action<EntityTypeBuilder> buildAction)
        {
            buildAction(Entity(type));

            return this;
        }

        public override ModelBuilder Entity([NotNull] string name, [NotNull] Action<EntityTypeBuilder> buildAction)
        {
            buildAction(Entity(name));

            return this;
        }

        public override QueryTypeBuilder<TQuery> Query<TQuery>()
        {
            return new QueryTypeBuilder<TQuery>(Instance.Query(typeof(TQuery), ConfigurationSource.Explicit));
        }

        public override QueryTypeBuilder Query([NotNull] Type type)
        {
            return new QueryTypeBuilder(Instance.Query(type, ConfigurationSource.Explicit));
        }

        public override ModelBuilder Query<TQuery>([NotNull] Action<QueryTypeBuilder<TQuery>> buildAction)
        {
            buildAction(Query<TQuery>());

            return this;
        }

        public override ModelBuilder Query([NotNull] Type type, [NotNull] Action<QueryTypeBuilder> buildAction)
        {
            buildAction(Query(type));

            return this;
        }

        public override ModelBuilder Ignore<TEntity>()
            => Ignore(typeof(TEntity));

        public override ModelBuilder Ignore([NotNull] Type type)
        {
            Instance.Ignore(type, ConfigurationSource.Explicit);

            return this;
        }

        public override ModelBuilder ApplyConfiguration<TEntity>([NotNull] IEntityTypeConfiguration<TEntity> configuration)
        {
            configuration.Configure(Entity<TEntity>());

            return this;
        }

        public override ModelBuilder ApplyConfiguration<TQuery>([NotNull] IQueryTypeConfiguration<TQuery> configuration)
        {
            configuration.Configure(Query<TQuery>());

            return this;
        }

        public override OwnedEntityTypeBuilder<T> Owned<T>()
        {
            Instance.Owned(typeof(T), ConfigurationSource.Explicit);

            return null;
        }

        public override OwnedEntityTypeBuilder Owned([NotNull] Type type)
        {
            Instance.Owned(type, ConfigurationSource.Explicit);

            return null;
        }

        public override ModelBuilder HasChangeTrackingStrategy(ChangeTrackingStrategy changeTrackingStrategy)
        {
            Instance.Metadata.ChangeTrackingStrategy = changeTrackingStrategy;

            return this;
        }

        public override ModelBuilder UsePropertyAccessMode(PropertyAccessMode propertyAccessMode)
        {
            Instance.UsePropertyAccessMode(propertyAccessMode, ConfigurationSource.Explicit);

            return this;
        }
    }
}
