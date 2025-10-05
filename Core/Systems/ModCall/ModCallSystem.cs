using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.ModLoader;
using ZensSky.Core.DataStructures;
using ZensSky.Core.Utils;

namespace ZensSky.Core.Systems.ModCall;

public sealed class ModCallSystem : ModSystem
{
    #region Private Fields

    private static readonly ModCallHandlers Handlers = [];

    #endregion

    #region Loading

    public override void Load()
    {
        Assembly assembly = Mod.Code;

        IEnumerable<MethodInfo> methods = assembly.GetAllDecoratedMethods<ModCallAttribute>();

        foreach (MethodInfo method in methods)
        {
            ModCallAttribute? attribute = method.GetCustomAttribute<ModCallAttribute>();

            if (attribute is null)
                continue;

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

    #endregion

    #region Public Methods

    public static object? HandleCall(string name, object?[]? arguments)
    {
        try
        {
            return Handlers.Invoke(name, arguments);
        }
        catch (KeyNotFoundException)
        {
            throw new ArgumentException($"{name} does not match any known method alias!");
        }
    }

    #endregion
}
