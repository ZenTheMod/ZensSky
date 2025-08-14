using System;
using System.Linq;
using System.Reflection;
using Terraria.ModLoader;
using ZensSky.Core.DataStructures;

namespace ZensSky.Core.Systems.ModCall;

public sealed class ModCallSystem : ModSystem
{
    private readonly static ModCallHandlers Handlers = [];

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

            if (attribute.NameAliases.Length <= 0)
                names = [m.Name];
            else if (attribute.UsesDefaultName)
                names = [m.Name, .. attribute.NameAliases];
            else
                names = attribute.NameAliases;

            Handlers.Add([.. names], m);
        }
    }

    public override void Unload() =>
        Handlers.Clear();

    public static object? HandleCall(string name, object?[]? arguments)
    {
        return Handlers.Invoke(name, arguments);

        throw new ArgumentException($"{name} does not match any known method alias!");
    }
}
