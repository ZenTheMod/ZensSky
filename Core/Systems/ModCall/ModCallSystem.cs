using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria.ModLoader;

namespace ZensSky.Core.Systems.ModCall;

public sealed class ModCallSystem : ModSystem
{
        // TODO: Have multiple names refer to multiple methods.
    private readonly static Dictionary<string, MethodInfo> Handlers = [];

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

            string name;

            if (string.IsNullOrEmpty(attribute.AlternameName))
                name = m.Name;
            else
                name = attribute.AlternameName;

            Handlers.Add(name, m);
        }
    }

    public override void Unload() =>
        Handlers.Clear();

    public static object? HandleCall(string name, object?[]? arguments)
    {
        if (!Handlers.TryGetValue(name, out MethodInfo? m))
            throw new ArgumentException($"{name} did not refer to a valid handler.");

        return m?.Invoke(null, arguments);
    }
}
