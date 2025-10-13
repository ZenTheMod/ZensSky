using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using ZensSky.Core.Utils;
using static System.Reflection.BindingFlags;

namespace ZensSky.Core.Config;

    // Possibly the dumbest thing ever written.
[Autoload(Side = ModSide.Client)]
public sealed class NoConfigLocalizationSystem : ModSystem
{
    #region Private Fields

    private delegate void orig_RegisterLocalizationKeysForMembers(Type type);
    private static Hook? PatchRegisterLocalizationKeysForMembers;

    private delegate void orig_RegisterLocalizationKeysForEnumMembers(Type type);
    private static Hook? PatchRegisterLocalizationKeysForEnumMembers;

    private delegate void orig_RegisterLocalizationKeysForMemberType(Type type, Assembly owningAssembly);
    private static Hook? PatchRegisterLocalizationKeysForMemberType;

    private static HashSet<Type> Types = [];

    #endregion

    #region Loading

    public override void Load()
    {
        Assembly assembly = Mod.Code;

        Types = [.. assembly.GetAllDecoratedTypes<NoConfigLocalizationAttribute>()];

        MethodInfo? registerLocalizationKeysForMembers = typeof(ConfigManager).GetMethod(nameof(ConfigManager.RegisterLocalizationKeysForMembers), NonPublic | Static);

        if (registerLocalizationKeysForMembers is not null)
            PatchRegisterLocalizationKeysForMembers = new(registerLocalizationKeysForMembers,
                StopLocalizationForMembers);

        MethodInfo? registerLocalizationKeysForEnumMembers = typeof(ConfigManager).GetMethod(nameof(ConfigManager.RegisterLocalizationKeysForEnumMembers), NonPublic | Static);

        if (registerLocalizationKeysForEnumMembers is not null)
            PatchRegisterLocalizationKeysForEnumMembers = new(registerLocalizationKeysForEnumMembers,
                StopLocalizationForEnumMembers);

        MethodInfo? registerLocalizationKeysForMemberType = typeof(ConfigManager).GetMethod(nameof(ConfigManager.RegisterLocalizationKeysForMemberType), NonPublic | Static);

        if (registerLocalizationKeysForMemberType is not null)
            PatchRegisterLocalizationKeysForMemberType = new(registerLocalizationKeysForMemberType,
                StopLocalizationForMemberType);
    }

    public override void Unload()
    {
        PatchRegisterLocalizationKeysForMembers?.Dispose();
        PatchRegisterLocalizationKeysForEnumMembers?.Dispose();
        PatchRegisterLocalizationKeysForMemberType?.Dispose();
    }

    private void StopLocalizationForMembers(orig_RegisterLocalizationKeysForMembers orig, Type type)
    {
        if (!Types.Contains(type))
            orig(type);
    }

    private void StopLocalizationForEnumMembers(orig_RegisterLocalizationKeysForEnumMembers orig, Type type)
    {
        if (!Types.Contains(type))
            orig(type);
    }

    private void StopLocalizationForMemberType(orig_RegisterLocalizationKeysForMemberType orig, Type type, Assembly owningAssembly)
    {
        if (!Types.Contains(type))
            orig(type, owningAssembly);
    }

    #endregion
}