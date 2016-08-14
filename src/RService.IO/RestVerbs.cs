using System;

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
}
