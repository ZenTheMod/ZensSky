using MonoMod.Cil;
using System;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace ZensSky.Common.Systems;

[Autoload(Side = ModSide.Client)]
public sealed class CaptureInMenuSystem : ModSystem
{
    public override void Load() => Main.QueueMainThreadAction(() => IL_Main.DoDraw += AllowCapturingOnMainMenu);

    public override void Unload() => Main.QueueMainThreadAction(() => IL_Main.DoDraw -= AllowCapturingOnMainMenu);

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
            Mod.Logger.Error("Failed to patch \"Main.DoDraw\".");

            throw new ILPatchFailureException(Mod, il, e);
        }
    }
}
