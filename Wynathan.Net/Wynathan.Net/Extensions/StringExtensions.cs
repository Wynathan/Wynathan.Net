using System;
using System.Runtime.CompilerServices;

namespace Wynathan.Net.Extensions
{
    internal static class StringExtensions
    {
        /// <summary>
        /// Returns a value indicating whether a specified <paramref name="value"/> 
        /// substring occurs within a <see cref="string"/> <paramref name="instance"/>.
        /// </summary>
        /// <param name="instance">
        /// A <see cref="string"/> to check.
        /// </param>
        /// <param name="value">
        /// A substring to verify occurance of within the <paramref name="instance"/>.
        /// </param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsII(this string instance, string value)
        {
            var instanceII = instance.ToUpperInvariant();
            var valueII = value.ToUpperInvariant();
            return instanceII.Contains(valueII);
        }

        /// <summary>
        /// Determines whether a <see cref="string"/> <paramref name="instance"/> and 
        /// a specified <see cref="string"/> <paramref name="value"/> have the 
        /// same value. Uses <see cref="StringComparison.InvariantCultureIgnoreCase"/> 
        /// underneath.
        /// </summary>
        /// <param name="instance">
        /// A <see cref="string"/> to compare.
        /// </param>
        /// <param name="value">
        /// A <see cref="string"/> to compare to.
        /// </param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsII(this string instance, string value)
        {
            return instance.Equals(value, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Determines whether the beginning of a <see cref="string"/> <paramref name="instance"/> 
        /// matches the specified <see cref="string"/> <paramref name="value"/>. Uses 
        /// <see cref="StringComparison.InvariantCultureIgnoreCase"/> underneath.
        /// </summary>
        /// <param name="instance">
        /// A <see cref="string"/> to compare the beginning of.
        /// </param>
        /// <param name="value">
        /// A <see cref="string"/> to compare to.
        /// </param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StartsWithII(this string instance, string value)
        {
            return instance.StartsWith(value, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Determines whether the end of a <see cref="string"/> <paramref name="instance"/> 
        /// matches the specified <see cref="string"/> <paramref name="value"/>. Uses 
        /// <see cref="StringComparison.InvariantCultureIgnoreCase"/> underneath.
        /// </summary>
        /// <param name="instance">
        /// A <see cref="string"/> to compare the end of.
        /// </param>
        /// <param name="value">
        /// A <see cref="string"/> to compare to.
        /// </param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EndsWithII(this string instance, string value)
        {
            return instance.EndsWith(value, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
