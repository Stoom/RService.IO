using System;
using System.Linq;
using System.Collections.Generic;

namespace RService.IO
{
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

    public static class RestVerbsExtensions
    {
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
