using Microsoft.Xna.Framework;
using Terraria;
using Terraria.UI;
using ZensSky.Core.UI;

namespace ZensSky.Common.Systems.Menu.Elements;

public abstract class SliderController : MenuController
{
    #region Private Fields

    protected const float DefaultHeight = 75f;

    #endregion

    #region Public Fields

    public readonly UISlider? Slider;

    #endregion

    #region Public Properties

    public abstract float MaxRange { get; }
    public abstract float MinRange { get; }

    public abstract Color InnerColor { get; }

    public abstract ref float Modifying {  get; }

    #endregion

    public SliderController()
        : base()
    {
        Height.Set(DefaultHeight, 0f);

        Slider = new();

        Slider.Top.Set(35f, 0f);

        Slider.InnerColor = InnerColor;

        MenuImageButton leftButton = new(ButtonTextures.ArrowLeft)
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

        MenuImageButton rightButton = new(ButtonTextures.ArrowRight)
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
