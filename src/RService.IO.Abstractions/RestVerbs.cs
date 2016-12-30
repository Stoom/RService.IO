using System;
using System.Collections.Generic;
using System.Linq;

namespace RService.IO.Abstractions
{
    /// <summary>
    /// The RESTful verbs supported by RServiceIO.
    /// </summary>
    [Flags]
    public enum RestVerbs
    {
        Get = 1,
        Post = 2,
        Put = 4,
        Patch = 8,
        Delete = 16,
        Options = 32,
        Any = Get | Post | Put | Patch | Delete | Options
    }

    /// <summary>
    /// Extensions to the <see cref="RestVerbs"/> enum.
    /// </summary>
    public static class RestVerbsExtensions
    {
        /// <summary>
        /// Gets an <see cref="IEnumerable{T}"/> of the verb strings.
        /// </summary>
        /// <param name="verbs"><see cref="RestVerbs"/> to get an enumeration of.</param>
        /// <returns>The <see cref="IEnumerable{T}"/> of verb <see cref="string"/>s.</returns>
        public static IEnumerable<string> ToEnumerable(this RestVerbs verbs)
        {
            if (verbs == RestVerbs.Any)
                return new[] { verbs.ToString().ToUpper() };

            return verbs
                .ToString()
                .ToUpper()
                .Split(',')
                .Select(x => x.Trim());
        }

        /// <summary>
        /// Returns <see cref="IEnumerable{T}"/> of enumeration flags.
        /// </summary>
        /// <param name="verbs">The flag enumeration.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of each flag.</returns>
        public static List<RestVerbs> GetFlags(this RestVerbs verbs)
        {
            return Enum.GetValues(verbs.GetType())
                .Cast<RestVerbs>()
                .Where(value => verbs.HasFlag(value) && ((ulong) value).IsPrimitiveFlag())
                .ToList();
        }

        /// <summary>
        /// Parses a <see cref="string"/> into a <see cref="RestVerbs"/> enumeration.
        /// </summary>
        /// <param name="method">The method to parse.</param>
        /// <returns>The <see cref="RestVerbs"/> flag matching the method.</returns>
        public static RestVerbs ParseRestVerb(this string method)
        {
            return (RestVerbs) Enum.Parse(typeof(RestVerbs), method, true);
        }

        /// <summary>
        /// Checks if an unsigned long is a power of 2.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns><b>True</b> if the <see cref="ulong"/> is the power of 2, else <b>false</b>.</returns>
        public static bool IsPrimitiveFlag(this ulong value)
        {
            return (value != 0) && ((value & (value - 1)) == 0);
        }
    }
}
