using System;
using System.Collections.Generic;
using System.Reflection;
using ZensSky.Core.Utils;

namespace ZensSky.Core.DataStructures;

public sealed class ModCallHandlers : AliasedList<string, MethodInfo>
{
    public ModCallHandlers()
        : base() { }

    public ModCallHandlers(MethodInfo item, Func<MethodInfo, HashSet<string>> nameFunc)
        : base([item], nameFunc) { }

    public object? Invoke(string name, object?[]? args)
    {
        int matching = this[name].FindIndex(m => m.MatchesArguments(args));

        if (matching != -1)
            return this[name][matching]?.Invoke(null, args);

        throw new ArgumentException($"No suitable method matching {args} was found!");
    }
}
