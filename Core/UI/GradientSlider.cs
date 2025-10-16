using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics.CodeAnalysis;
using Terraria;
using Terraria.UI;
using ZensSky.Core.DataStructures;
using ZensSky.Core.Utils;

namespace ZensSky.Core.UI;

public class GradientSlider : UISlider
{
    #region Public Events

        // TODO: Generic impl of UIElementAction.
    public event Action<GradientSlider>? OnSegmentSelected;

    #endregion

    #region Public Properties

    public Gradient Gradient { get; init; }

    public GradientSegment? HoveredSegment { get; private set; }

    public GradientSegment TargetSegment { get; private set; }

    #endregion

    #region Public Constructors

    public GradientSlider(Gradient gradient)
        : base() 
    {
        Gradient = gradient;
        TargetSegment = Gradient[0];
    }

    #endregion

    #region Interactions

    public override void LeftMouseDown(UIMouseEvent evt)
    {
        base.LeftMouseDown(evt);

        IsHeld = false;

        if (evt.Target != this ||
            Main.alreadyGrabbingSunOrMoon)
            return;

        Rectangle dims = this.Dimensions;

        float ratio = Utilities.Saturate((evt.MousePosition.X - dims.X) / dims.Width);

        if (HoveredSegment is not null)
            TargetSegment = HoveredSegment;
        else if (Gradient.Count < Gradient.MaxColors)
        {
            Color color = Gradient.GetColor(ratio);

            GradientSegment newSegment = new(ratio, color);

            Gradient.Add(newSegment);

            TargetSegment = newSegment;
        }
        else
            return;

        OnSegmentSelected?.Invoke(this);

        IsHeld = true;
    }

    public override void RightMouseDown(UIMouseEvent evt)
    {
        base.RightMouseDown(evt);

        if (evt.Target != this ||
            Main.alreadyGrabbingSunOrMoon ||
            Gradient.Count <= 2)
            return;

        Rectangle dims = this.Dimensions;

        float ratio = Utilities.Saturate((evt.MousePosition.X - dims.X) / dims.Width);

        if (HoveredSegment is not null)
        {
            Gradient.Remove(HoveredSegment);

            TargetSegment = Gradient[0];

            OnSegmentSelected?.Invoke(this);
        }
    }

    #endregion

    #region Updating

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        GetHoveredSegment(out GradientSegment? hovered);

        HoveredSegment = hovered;

        if (IsHeld)
            TargetSegment.Position = Ratio;
    }

    #endregion

    #region Drawing

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        Rectangle dims = this.Dimensions;

        DrawBars(spriteBatch, dims);

        dims.Inflate(-4, -4);

        DrawGradient(spriteBatch, dims);

        DrawBlips(spriteBatch, dims);
    }

    protected void DrawGradient(SpriteBatch spriteBatch, Rectangle dims)
    {
        for (int i = 0; i < dims.Width; i++)
        {
            Rectangle segement = new(dims.X + i, dims.Y, 1, dims.Height);

            Color color = Gradient.GetColor(i / (float)dims.Width);

            spriteBatch.Draw(MiscTextures.Pixel, segement, color);
        }
    }

    protected void DrawBlips(SpriteBatch spriteBatch, Rectangle dims)
    {
        foreach (GradientSegment segment in Gradient)
        {
            Color color = segment == HoveredSegment ? Color.White : Color.Gray;

            color = segment == TargetSegment ? Main.OurFavoriteColor : color;

            DrawBlip(spriteBatch, dims, segment.Position, color);
        }
    }

    #endregion

    #region Private Methods
    
    protected bool GetHoveredSegment([NotNullWhen(true)] out GradientSegment? segment)
    {
        segment = null;

        if (!IsMouseHovering)
            return false;

        Rectangle dims = this.Dimensions;

        float ratio = Utilities.Saturate((Utilities.UIMousePosition.X - dims.X) / dims.Width);

        GradientSegment nearest = Gradient.CompareFor((s) => Math.Abs(s.Position - ratio), out float dist, false);

        if (dist < 8f / dims.Width)
        {
            segment = nearest;

            return true;
        }

        return false;
    }


    #endregion
}
