using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Wynathan.Net.Extensions
{
    public static class EnumerableExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> collection)
        {
            return collection == null || !collection.Any();
        }
    }
}
