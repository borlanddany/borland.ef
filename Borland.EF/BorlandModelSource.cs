using System;
using System.Collections.Concurrent;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Borland.EF
{
    public class BorlandModelSource : ModelSource
    {
        private readonly ConcurrentDictionary<object, Lazy<IModel>> _models;

        public BorlandModelSource([NotNull] ModelSourceDependencies dependencies) : base(dependencies)
        {
            _models = new ConcurrentDictionary<object, Lazy<IModel>>();
        }

        protected override IModel CreateModel(
           [NotNull] DbContext context,
           [NotNull] IConventionSetBuilder conventionSetBuilder,
           [NotNull] IModelValidator validator)
        {
            var conventionSet = CreateConventionSet(conventionSetBuilder);

            var modelBuilder = new BorlandModelBuilder(conventionSet, context);
            var model = modelBuilder.GetInfrastructure().Metadata;
            model[CoreAnnotationNames.ProductVersionAnnotation] = ProductInfo.GetVersion();

            Dependencies.ModelCustomizer.Customize(modelBuilder, context);

            model.Validate();

            validator.Validate(model);

            return model;
        }
    }
}
