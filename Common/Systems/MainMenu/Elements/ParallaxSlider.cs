using Microsoft.Xna.Framework;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.Config;

namespace ZensSky.Common.Systems.MainMenu.Elements;

public sealed class ParallaxSlider : MenuControllerElement
{
    #region Fields

    private const float MaxRange = 5f;

    private readonly UISlider? Slider;

    public override int Index => 2;

    public override string Name => "Mods.ZensSky.MenuController.Parallax";

    #endregion

    public ParallaxSlider() : base()
    {
        Height.Set(75f, 0f);

        Slider = new();

        Slider.Top.Set(35f, 0f);

        Append(Slider);
    }

    #region Loading

    public override void OnLoad() => Main.QueueMainThreadAction(() => IL_Main.DrawMenu += ChangeParallaxDirection);

    public override void OnUnload() => Main.QueueMainThreadAction(() => IL_Main.DrawMenu += ChangeParallaxDirection);

    private void ChangeParallaxDirection(ILContext il)
    {
        try 
        { 
            ILCursor c = new(il);

            c.GotoNext(MoveType.Before,
                i => i.MatchStsfld<Main>(nameof(Main.MenuXMovement)));

            c.EmitPop();

            c.EmitDelegate(() => MenuConfig.Instance.Parallax);
        }
        catch (Exception e)
        {
            ModContent.GetInstance<ZensSky>().Logger.Error("Failed to patch \"Main.DrawMenu\".");

            throw new ILPatchFailureException(ModContent.GetInstance<ZensSky>(), il, e);
        }
    }

    #endregion

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (Slider is null)
            return;

        if (Slider.IsHeld)
            MenuConfig.Instance.Parallax = Utils.Remap(Slider.Ratio, 0, 1, MaxRange, -MaxRange);
        else
            Slider.Ratio = Utils.Remap(MenuConfig.Instance.Parallax, MaxRange, -MaxRange, 0, 1);
    }
}
