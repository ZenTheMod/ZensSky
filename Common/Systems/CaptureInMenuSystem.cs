using MonoMod.Cil;
using System;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;
using ZensSky.Core.Exceptions;
using ZensSky.Core.Systems;

namespace ZensSky.Common.Systems;

[Autoload(Side = ModSide.Client)]
public sealed class CaptureInMenuSystem : ModSystem
{
    #region Loading

    public override void Load()
    {
        MainThreadSystem.Enqueue(() =>
        {
            IL_Main.DoDraw += AllowCapturingOnMainMenu;
            IL_Main.ClearVisualPostProcessEffects += DontDisableEffectsOnMenu;
        });
    }

    public override void Unload()
    {
        MainThreadSystem.Enqueue(() =>
        {
            IL_Main.DoDraw -= AllowCapturingOnMainMenu;
            IL_Main.ClearVisualPostProcessEffects -= DontDisableEffectsOnMenu;
        });
    }

    #endregion

    #region Allow Capturing

    private void AllowCapturingOnMainMenu(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            ILLabel? jumpEndCaptureTarget = c.DefineLabel();

            int menuCaptureFlagIndex = -1;
            int shouldCaptureIndex = -1;

                // Allow capturing to start; will only take effect if there are any active filters present.
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

            c.EmitDelegate(() => ZensSky.Unloading);
            c.EmitStloc(menuCaptureFlagIndex);

                // Grab flag2's index.
            c.GotoNext(MoveType.After, 
                i => i.MatchBr(out _),
                i => i.MatchLdcI4(0),
                i => i.MatchStloc(out shouldCaptureIndex),
                i => i.MatchLdloc(shouldCaptureIndex));

                // Move EndCapture to before UI drawing is done, I wouldn't suspect it to be the brightest idea to have it affect all UI.
            c.GotoNext(MoveType.Before,
                i => i.MatchLdarg(out _),
                i => i.MatchLdloca(out _),
                i => i.MatchLdloca(out _),
                i => i.MatchCall<Main>(nameof(Main.PreDrawMenu)));

            c.MoveAfterLabels();

            c.EmitLdloc(shouldCaptureIndex);
            c.EmitDelegate((bool capture) =>
            {
                if (!capture)
                    return;

                Filters.Scene.EndCapture(null, Main.screenTarget, Main.screenTargetSwap, Color.Black);
            });

                // And branch over vanilla.
            c.GotoNext(MoveType.Before,
                i => i.MatchLdloc(shouldCaptureIndex),
                i => i.MatchBrfalse(out jumpEndCaptureTarget));

            c.EmitBr(jumpEndCaptureTarget);
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    #endregion

    #region Filter Deactivation

    private void DontDisableEffectsOnMenu(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            ILLabel jumpResettingTarget = c.DefineLabel();

                // While we want filters (and rain) to not be force disabled, some effects should remain disabled here.
            c.GotoNext(MoveType.After,
                i => i.MatchCall<CreditsRollEvent>(nameof(CreditsRollEvent.Reset)));

            c.EmitBr(jumpResettingTarget);

                // This will still disable all CustomSkys, as some may be unwanted on the menu.
            c.GotoNext(MoveType.Before,
                i => i.MatchLdsfld<SkyManager>(nameof(SkyManager.Instance)),
                i => i.MatchCallvirt<SkyManager>(nameof(SkyManager.DeactivateAll)));

            c.MarkLabel(jumpResettingTarget);
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    #endregion
}
