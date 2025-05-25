using Microsoft.Xna.Framework;
using Terraria;
using ZensSky.Common.Config;

namespace ZensSky.Common.Systems.MainMenu.Elements;

public sealed class WindSlider : MenuControllerElement
{
    #region Fields

    private const float MaxRange = 1f;

    private readonly UISlider? Slider;

    public override int Index => 3;

    public override string Name => "Mods.ZensSky.MenuController.Wind";

    #endregion

    public WindSlider() : base()
    {
        Height.Set(75f, 0f);

        Slider = new();

        Slider.Top.Set(35f, 0f);

        Append(Slider);
    }

    public override void Refresh() 
    {
        if (MenuConfig.Instance.UseWind)
            Main.windSpeedCurrent = MenuConfig.Instance.Wind; 
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (Slider is null)
            return;

        if (Slider.IsHeld)
        {
            MenuConfig.Instance.Wind = Utils.Remap(Slider.Ratio, 0, 1, -MaxRange, MaxRange);
            MenuConfig.Instance.UseWind = true;
            Refresh();
        }
        else
            Slider.Ratio = Utils.Remap(MenuConfig.Instance.Wind, -MaxRange, MaxRange, 0, 1);
    }
}
