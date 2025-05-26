using Microsoft.Xna.Framework;
using Terraria;
using ZensSky.Common.Config;

namespace ZensSky.Common.Systems.MainMenu.Elements;

public sealed class RainSlider : MenuControllerElement
{
    #region Fields

    private readonly UISlider? Slider;

    public override int Index => 4;

    public override string Name => "Mods.ZensSky.MenuController.Rain";

    #endregion

    public RainSlider() : base()
    {
        Height.Set(75f, 0f);

        Slider = new();

        Slider.Top.Set(35f, 0f);

        Slider.InnerColor = Color.Blue;

        Append(Slider);
    }

    public override void Refresh() 
    {
        Main.maxRaining = MenuConfig.Instance.Rain;

        Main.cloudAlpha = MenuConfig.Instance.Rain;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (Slider is null)
            return;

        if (Slider.IsHeld)
        {
            MenuConfig.Instance.Rain = Slider.Ratio;
            Refresh();
        }
        else
            Slider.Ratio = Main.cloudAlpha;
    }
}
