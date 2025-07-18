using System;
using System.Collections.Generic;
using System.Reflection;
using ZensSky.Core.Utils;

namespace ZensSky.Core.Systems.ModCall;

public readonly record struct ModCallAlias
{
    public readonly string[] Names { get; init; }

    public readonly List<MethodInfo> Methods { get; init; }

    public ModCallAlias(string[] names, MethodInfo method)
    {
        Names = names;
        Methods = [method];
    }

    public void Add(MethodInfo method) =>
        Methods.Add(method);

    public object? Invoke(object?[]? args)
    {
        int matching = Methods.FindIndex(m => m.MatchesArguments(args));

        if (matching != -1)
            return Methods[matching]?.Invoke(null, args);

        throw new ArgumentException($"No suitable method matching {args} was found!");
    }
}
