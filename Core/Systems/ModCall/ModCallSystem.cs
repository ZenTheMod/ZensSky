using System;
using System.Linq;
using System.Reflection;
using Terraria.ModLoader;
using ZensSky.Core.DataStructures;
using static System.Reflection.BindingFlags;

namespace ZensSky.Core.Systems.ModCall;

public sealed class ModCallSystem : ModSystem
{
    private readonly static ModCallHandlers Handlers = [];

    public override void Load()
    {
        Assembly assembly = Mod.Code;

        MethodInfo[]? methods = [.. assembly.GetTypes()
            .SelectMany(t => t.GetMethods(Public | NonPublic | Static))
            .Where(m => m.GetCustomAttribute<ModCallAttribute>() is not null)];

        foreach (MethodInfo method in methods)
        {
            if (method.IsGenericMethod)
                continue;

            ModCallAttribute? attribute = method.GetCustomAttribute<ModCallAttribute>();

            if (attribute is null)
                return;

            string[] names;

            if (attribute.NameAliases.Length <= 0)
                names = [method.Name];
            else if (attribute.UsesDefaultName)
                names = [method.Name, .. attribute.NameAliases];
            else
                names = attribute.NameAliases;

            Handlers.Add([.. names], method);
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
