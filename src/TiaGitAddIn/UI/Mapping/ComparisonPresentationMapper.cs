using System;
using System.Collections.Generic;
using System.Linq;
using TiaGitAddIn.Models.Comparison;
using TiaGitAddIn.UI.ViewModels.Comparison;

namespace TiaGitAddIn.UI.Mapping
{
    /// <summary>
    /// Aggregates every registered <see cref="IComparisonPresentationViewModelFactory"/> and routes
    /// each <see cref="PlcComparisonResult"/> to the single factory whose <see cref="IComparisonPresentationViewModelFactory.CanMap"/>
    /// matches its presentation. Computes <see cref="ComparisonViewModelMetadata"/> exactly once per
    /// call so every specialized view model shares the identical header/limitation/diagnostics/raw-text
    /// snapshot. Requires exactly one compatible factory -- zero or multiple is a wiring bug, not a
    /// runtime condition to tolerate, so both throw <see cref="InvalidOperationException"/>.
    /// </summary>
    public sealed class ComparisonPresentationMapper : IComparisonPresentationMapper
    {
        private readonly IReadOnlyList<IComparisonPresentationViewModelFactory> factories;

        public ComparisonPresentationMapper(IEnumerable<IComparisonPresentationViewModelFactory> factories)
        {
            if (factories == null) throw new ArgumentNullException(nameof(factories));
            this.factories = factories.ToArray();
        }

        public ComparisonPresentationViewModel Map(PlcComparisonResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            ComparisonViewModelMetadata metadata = ComparisonViewModelMetadata.From(result);
            IComparisonPresentationViewModelFactory[] matches = factories
                .Where(factory => factory.CanMap(result.Presentation))
                .ToArray();

            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected exactly one factory able to map presentation kind '{result.Presentation.Kind}' " +
                    $"({result.Presentation.GetType().Name}), found {matches.Length}.");
            }

            return matches[0].Map(result, metadata);
        }
    }
}
