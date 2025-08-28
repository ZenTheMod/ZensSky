using System;
using System.Reflection;
using ZensSky.Core.Utils;

namespace ZensSky.Core.DataStructures;

public sealed class ModCallHandlers : AliasedList<string, MethodInfo>
{
        // TODO: Allow non static methods to be invoked.
    public object? Invoke(string name, object?[]? args)
    {
        int matching = this[name].FindIndex(m => m.MatchesParameters(args));

        if (matching != -1)
            return this[name][matching]?.Invoke(null, args);

        throw new ArgumentException($"No suitable method matching {args} was found!");
    }
}
