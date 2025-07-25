﻿using Microsoft.Xna.Framework;
using Terraria;
using Terraria.UI;
using ZensSky.Common.Registries;

namespace ZensSky.Common.Systems.Menu.Elements;

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

        FixedImageButton leftButton = new(Textures.ArrowLeft)
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

        leftButton.OnMouseOver += DisableHoveringWhileGrabbingSunOrMoon;

        FixedImageButton rightButton = new(Textures.ArrowRight)
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

        rightButton.OnMouseOver += DisableHoveringWhileGrabbingSunOrMoon;

        Append(leftButton);
        Append(rightButton);

        Append(Slider);
    }

    private void DisableHoveringWhileGrabbingSunOrMoon(UIMouseEvent evt, UIElement listeningElement) =>
        listeningElement.IsMouseHovering = !Main.alreadyGrabbingSunOrMoon;

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
