using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.Config;

namespace ZensSky.Common.Systems.MainMenu.Elements;

public sealed class TimeController : SliderController
{
    #region Properties

    public override float MaxRange => 20f;
    public override float MinRange => 0.1f;

    public override Color InnerColor => Color.MediumPurple;

    public override ref float Modifying => ref MenuConfig.Instance.TimeMultiplier;

    public override int Index => 5;

    public override string Name => "Mods.ZensSky.MenuController.Time";

    #endregion

    #region Loading

    public override void OnLoad() => Main.QueueMainThreadAction(() => IL_Main.UpdateMenu += ChangeParallaxDirection);

    public override void OnUnload() => Main.QueueMainThreadAction(() => IL_Main.UpdateMenu -= ChangeParallaxDirection);

    private void ChangeParallaxDirection(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            c.GotoNext(MoveType.After,
                i => i.MatchLdcR8(33.88235294117647)); // I shit you not this is the hardcoded value.

            c.EmitDelegate((double time) => time * MenuConfig.Instance.TimeMultiplier);

            c.GotoNext(MoveType.After,
                i => i.MatchLdcR8(30.857142857142858)); // I shit you not this is the hardcoded value.

            c.EmitDelegate((double time) => time * MenuConfig.Instance.TimeMultiplier);
        }
        catch (Exception e)
        {
            ModContent.GetInstance<ZensSky>().Logger.Error("Failed to patch \"Main.UpdateMenu\".");

            throw new ILPatchFailureException(ModContent.GetInstance<ZensSky>(), il, e);
        }
    }

    #endregion
}
