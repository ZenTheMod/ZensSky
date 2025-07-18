using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria.ModLoader;

namespace ZensSky.Core.Systems.ModCall;

public sealed class ModCallSystem : ModSystem
{
        // TODO: Have multiple names refer to multiple methods.
    private readonly static List<ModCallAlias> Handlers = [];

    public override void Load()
    {
        Assembly assembly = Mod.Code;

        MethodInfo[]? methods = [.. assembly.GetTypes()
            .SelectMany(t => t.GetMethods())
            .Where(m => m.GetCustomAttributes(typeof(ModCallAttribute), false).Length > 0 && m.IsStatic)];

        foreach (MethodInfo m in methods)
        {
            ModCallAttribute? attribute = m.GetCustomAttribute<ModCallAttribute>();

            if (attribute is null)
                return;

            string[] names;

            if (attribute.AlternameNames.Length <= 0)
                names = [m.Name];
            else
                names = [m.Name, .. attribute.AlternameNames];

            int inList = Handlers.FindIndex(a => a.Names == names);

            if (inList != -1)
                Handlers[inList].Add(m);
            else
                Handlers.Add(new(names, m));
        }
    }

    public override void Unload() =>
        Handlers.Clear();

    public static object? HandleCall(string name, object?[]? arguments)
    {
        int matching = Handlers.FindIndex(a => a.Names.Contains(name));

        if (matching != -1)
            return Handlers[matching].Invoke(arguments);

        throw new ArgumentException($"{name} does not match any know method alias!");
    }
}
