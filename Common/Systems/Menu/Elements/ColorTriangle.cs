using Daybreak.Common.Features.Hooks;
using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using ZensSky.Common.Registries;
using ZensSky.Core.Utils;

namespace ZensSky.Common.Systems.Menu.Elements;

public sealed class ColorTriangle : UIElement
{
    #region Private Fields

    private static readonly Color Outline = new(215, 215, 215);

    private readonly Vector2[] Points = new Vector2[9];

    #endregion

    #region Public Fields

    public float Hue;

    public Vector2 PickerPosition;

    public bool IsHeld;

    public static readonly Vector2[] NormalizedPoints = new Vector2[3];

    #endregion

    #region Properties

    private bool Hovering =>
        ContainsPoint(Utilities.UIMousePosition) && !Main.alreadyGrabbingSunOrMoon && Parent.IsMouseHovering;

    #endregion

    public ColorTriangle() =>
        Width.Set(0, 1f);

    #region Loading

    [OnLoad(Side = ModSide.Client)]
    public static void Load()
    {
        float theta = -MathHelper.PiOver2;
        float thetaInc = MathHelper.TwoPi / 3f;
        for (int i = 0; i < NormalizedPoints.Length; i++)
        {
            NormalizedPoints[i].X = MathF.Cos(theta) * .5f;
            NormalizedPoints[i].Y = MathF.Sin(theta) * .5f;
            theta += thetaInc;
        }
    }

    #endregion

    #region Updating

    public override void LeftMouseDown(UIMouseEvent evt)
    {
        base.LeftMouseDown(evt);

        if (Main.alreadyGrabbingSunOrMoon)
            return;

        if (evt.Target == this)
            IsHeld = true;
    }

    public override void LeftMouseUp(UIMouseEvent evt)
    {
        base.LeftMouseUp(evt);
        IsHeld = false;
    }

    public override void MouseOver(UIMouseEvent evt)
    {
        base.MouseOver(evt);

        IsMouseHovering = !Main.alreadyGrabbingSunOrMoon;

        if (!IsMouseHovering || IsHeld)
            return;

        SoundEngine.PlaySound(SoundID.MenuTick);
    }

    public override bool ContainsPoint(Vector2 point) =>
        Utilities.IsPointInTriangle(point, Points);

    public override void Recalculate()
    {
        base.Recalculate();

        CalculatedStyle dims = GetDimensions();

        Vector2 center = dims.Center();
        float diameter = dims.Width;
        diameter -= 16;

        for (int i = 0; i < Points.Length; i++)
        {
            Points[i] = center + NormalizedPoints[i % 3] * diameter;

            if ((i + 1) % 3 == 0)
                diameter += 8;
        }
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (!IsHeld)
            return;

        CalculatedStyle dims = GetDimensions();

        Vector2 position = Utilities.ClosestPointOnTriangle(Utilities.UIMousePosition, Points) - dims.Center();

        PickerPosition = position / dims.Size();
    }

    #endregion

    #region Drawing

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        CalculatedStyle dims = GetDimensions();

        spriteBatch.End(out var snapshot);

        GraphicsDevice device = Main.instance.GraphicsDevice;

            // VertexPositionColor throws ??
        VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[Points.Length];

        for (int i = 0; i < Points.Length; i++)
            vertices[Points.Length - 1 - i] = new(new(Points[i], 0), GetColor(i), new(.5f));

        device.Textures[0] = Textures.Pixel.Value;

        device.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, Points.Length / 3);

        spriteBatch.Begin(in snapshot);

        Texture2D picker = Textures.Dot.Value;

        Vector2 pickerOrigin = picker.Size() * .5f;

        Vector2 position = PickerPosition * dims.Size() + dims.Center();

        spriteBatch.Draw(picker, Utils.Round(position), null, Color.White, 0f, pickerOrigin, 1f, SpriteEffects.None, 0f);
    }

    #endregion

    #region Private Methods

    private Color[] GetColors() =>
        GetColors(Hue);

    private Color GetColor(int i)
    {
        if (i < 3)
            return GetColors()[i];
        else if (i < 6)
            return Hovering || IsHeld ? Main.OurFavoriteColor : Outline;
        else
            return Color.Black;
    }

    #endregion

    #region Public Methods

    public static Color[] GetColors(float hue) =>
        [Main.hslToRgb(hue, 1f, .5f), Color.Black, Color.White];

    #endregion
}
