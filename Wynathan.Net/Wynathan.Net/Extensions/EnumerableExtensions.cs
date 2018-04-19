using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Wynathan.Net.Extensions
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Verifies whether <paramref name="collection"/> is either null or 
        /// has no elements.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <returns>
        /// True, if collection either null or empty. Otherwise, false.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> collection)
        {
            return collection == null || !collection.Any();
        }
    }
}
