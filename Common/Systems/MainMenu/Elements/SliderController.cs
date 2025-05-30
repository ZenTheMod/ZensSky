using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;
using ZensSky.Common.Registries;

namespace ZensSky.Common.Systems.MainMenu.Elements;

public abstract class SliderController : MenuControllerElement
{
    public readonly UISlider? Slider;

    public abstract float MaxRange { get; }
    public abstract float MinRange { get; }

    public abstract Color InnerColor { get; }

    public abstract ref float Modifying {  get; }

    public SliderController() : base()
    {
        Height.Set(75f, 0f);

        Slider = new();

        Slider.Top.Set(35f, 0f);

        Slider.InnerColor = InnerColor;

        UIImageButton leftButton = new(Textures.ArrowLeft)
        {
            HAlign = 0f
        };

        leftButton.Width.Set(14f, 0f);
        leftButton.Height.Set(14f, 0f);
        leftButton.Top.Set(16f, 0f);

        leftButton.OnLeftMouseDown += (evt, listeningElement) => 
        { 
            Slider.Ratio = 0f;

            Modifying = MinRange;

            OnSet();
            Refresh();
        };

        UIImageButton rightButton = new(Textures.ArrowRight)
        {
            HAlign = 1f
        };

        rightButton.Width.Set(14f, 0f);
        rightButton.Height.Set(14f, 0f);
        rightButton.Top.Set(16f, 0f);

        rightButton.OnLeftMouseDown += (evt, listeningElement) => 
        {
            Slider.Ratio = 1f;

            Modifying = MaxRange;

            OnSet();
            Refresh();
        };

        Append(leftButton);
        Append(rightButton);

        Append(Slider);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (Slider is null)
            return;

        if (Slider.IsHeld)
        {
            Modifying = Utils.Remap(Slider.Ratio, 0, 1, MinRange, MaxRange);

            OnSet();
            Refresh();
        }
        else
            Slider.Ratio = Utils.Remap(Modifying, MinRange, MaxRange, 0, 1);
    }

    public virtual void OnSet() { }
}
