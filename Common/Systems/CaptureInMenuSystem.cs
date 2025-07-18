using MonoMod.Cil;
using System;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;
using ZensSky.Core.Exceptions;
using ZensSky.Core.Systems;

namespace ZensSky.Common.Systems;

[Autoload(Side = ModSide.Client)]
public sealed class CaptureInMenuSystem : ModSystem
{
    public override void Load() =>
        MainThreadSystem.Enqueue(() => IL_Main.DoDraw += AllowCapturingOnMainMenu);

    public override void Unload() =>
        MainThreadSystem.Enqueue(() => IL_Main.DoDraw -= AllowCapturingOnMainMenu);

    private void AllowCapturingOnMainMenu(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            int menuCaptureFlagIndex = -1;

            c.GotoNext(MoveType.After,
                i => i.MatchCallvirt<EffectManager<Filter>>("get_Item"),
                i => i.MatchCallvirt<Filter>(nameof(Filter.IsInUse)),
                i => i.MatchBrfalse(out _));

            c.GotoNext(MoveType.After,
                i => i.MatchLdcI4(0),
                i => i.MatchStloc(out menuCaptureFlagIndex));

            c.GotoNext(MoveType.Before,
                i => i.MatchLdsfld<Main>(nameof(Main.drawToScreen)));

            c.MoveAfterLabels();

            c.EmitLdcI4(0);
            c.EmitStloc(menuCaptureFlagIndex);
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }
}
