using Microsoft.Xna.Framework;
using Terraria;

namespace ZensSky.Common.Systems.MainMenu.Elements;

public sealed class CloudDensitySlider : MenuControllerElement
{
    public override int Index => 1;

    public override string Name => "Mods.ZensSky.MenuController.CloudDensity";

    private readonly UISlider? Slider;

    public CloudDensitySlider() : base()
    {
        Height.Set(75f, 0f);

        Slider = new();

        Slider.Top.Set(35f, 0f);

        Append(Slider);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (Slider is not null)
        {
            if (Slider.IsHeld)
            {
                int prior = Main.numClouds;
                Main.numClouds = (int)(Slider.Ratio * Main.maxClouds);

                Main.cloudBGActive = Utils.Remap(Slider.Ratio, 0.75f, 1f, 0f, 1f);

                if (Main.numClouds != prior)
                    Cloud.resetClouds();
            }
            else
                Slider.Ratio = (float)Main.numClouds / Main.maxClouds;
        }
    }
}
