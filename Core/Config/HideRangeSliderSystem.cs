using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.ModLoader;
using Terraria.ModLoader.Config.UI;
using ZensSky.Core.Exceptions;
using ZensSky.Core.Utils;
using static System.Reflection.BindingFlags;

namespace ZensSky.Core.Config;

[Autoload(Side = ModSide.Client)]
public sealed class HideRangeSliderSystem : ModSystem
{
    #region Private Fields

    private static ILHook? PatchDrawSelf;

    private static HashSet<Type> Types = [];

    #endregion

    #region Loading

    public override void Load()
    {
        Assembly assembly = Mod.Code;

        Types = [.. assembly.GetAllDecoratedTypes<HideRangeSliderAttribute>()];

        MethodInfo? drawSelf = typeof(RangeElement).GetMethod("DrawSelf", NonPublic | Instance);

        if (drawSelf is not null)
            PatchDrawSelf = new(drawSelf,
                SkipRangeElementDrawing);
    }

    public override void Unload() =>
        PatchDrawSelf?.Dispose();

    private void SkipRangeElementDrawing(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            ILLabel jumpret = c.DefineLabel();

            int elementIndex = -1;

            c.GotoNext(MoveType.After,
                i => i.MatchLdarg(out elementIndex),
                i => i.MatchLdarg(out _),
                i => i.MatchCall<ConfigElement>("DrawSelf"));

            c.EmitLdarg(elementIndex);

            c.EmitDelegate((RangeElement element) =>
                Types.Contains(element.GetType()));

            c.EmitBrfalse(jumpret);

            c.EmitRet();

            c.MarkLabel(jumpret);
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    #endregion
}
