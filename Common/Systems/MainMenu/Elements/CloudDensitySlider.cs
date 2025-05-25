using Microsoft.Xna.Framework;
using Terraria;
using ZensSky.Common.Config;

namespace ZensSky.Common.Systems.MainMenu.Elements;

public sealed class CloudDensitySlider : MenuControllerElement
{
    private readonly UISlider? Slider;

    public override int Index => 1;

    public override string Name => "Mods.ZensSky.MenuController.CloudDensity";

    public CloudDensitySlider() : base()
    {
        Height.Set(75f, 0f);

        Slider = new();

        Slider.Top.Set(35f, 0f);

        Append(Slider);
    }

    public override void Refresh()
    {
        if (MenuConfig.Instance.UseCloudDensity)
        {
            float density = MenuConfig.Instance.CloudDensity;
            Main.numClouds = (int)(density * Main.maxClouds);

            Main.cloudBGActive = Utils.Remap(density, 0.75f, 1f, 0f, 1f);
        }
        else if (Slider is not null)
            Slider.Ratio = (float)Main.numClouds / Main.maxClouds;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (Slider is null)
            return;

        if (Slider.IsHeld)
        {
            float density = Slider.Ratio;

            MenuConfig.Instance.UseCloudDensity = true;
            MenuConfig.Instance.CloudDensity = density;

            int prior = Main.numClouds;
            Main.numClouds = (int)(density * Main.maxClouds);

            Main.cloudBGActive = Utils.Remap(density, 0.75f, 1f, 0f, 1f);

            if (Main.numClouds != prior)
                Cloud.resetClouds();
        }
        else
            Slider.Ratio = MenuConfig.Instance.CloudDensity;

    }
}
