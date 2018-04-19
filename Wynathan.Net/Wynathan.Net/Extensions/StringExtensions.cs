using System;
using System.Runtime.CompilerServices;

using Wynathan.Net.Http.Models;

namespace Wynathan.Net.Extensions
{
    internal static class StringExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsII(this string instance, string value)
        {
            var instanceII = instance.ToUpperInvariant();
            var valueII = value.ToUpperInvariant();
            return instanceII.Contains(valueII);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsII(this string instance, string value)
        {
            return instance.Equals(value, StringComparison.InvariantCultureIgnoreCase);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StartsWithII(this string instance, string value)
        {
            return instance.StartsWith(value, StringComparison.InvariantCultureIgnoreCase);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EndsWithII(this string instance, string value)
        {
            return instance.EndsWith(value, StringComparison.InvariantCultureIgnoreCase);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsII(this string instance, HttpPort port)
        {
            return instance.EqualsII(port.ToString());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StartsWithII(this string instance, HttpPort port)
        {
            return instance.StartsWithII(port.ToString());
        }
    }
}
