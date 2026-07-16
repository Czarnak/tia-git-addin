using System;
using System.Collections.Generic;
using System.Linq;

namespace TiaGitAddIn.Models.Comparison
{
    /// <summary>
    /// Shared null-check-then-defensive-copy helper for immutable comparison model constructors
    /// that accept an <see cref="IEnumerable{T}"/> and must expose a read-only snapshot.
    /// </summary>
    internal static class ImmutableCopy
    {
        public static IReadOnlyList<T> Of<T>(IEnumerable<T> source, string paramName)
            => (source ?? throw new ArgumentNullException(paramName)).ToArray();
    }
}
