using System;
using System.Linq;
using System.Collections.Generic;

namespace RService.IO
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
    }
}
