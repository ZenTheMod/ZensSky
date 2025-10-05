using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader.Config.UI;

namespace ZensSky.Core.Utils;

public static partial class Utilities
{
    #region Private Fields

    private const float SliderWidth = 167f;

    #endregion

    #region RenderTargets

    /// <summary>
    /// Reinitializes <paramref name="target"/> if needed.
    /// </summary>
    public static void ReintializeTarget(ref RenderTarget2D? target, 
        GraphicsDevice device,
        int width,
        int height,
        bool mipMap = false,
        SurfaceFormat preferredFormat = SurfaceFormat.Color,
        DepthFormat preferredDepthFormat = DepthFormat.None,
        int preferredMultiSampleCount = 0,
        RenderTargetUsage usage = RenderTargetUsage.PreserveContents)
    {
        if (target is null ||
            target.IsDisposed ||
            target.Width != width ||
            target.Height != height)
        {
            target?.Dispose();
            target = new(device,
                width,
                height,
                mipMap,
                preferredFormat,
                preferredDepthFormat,
                preferredMultiSampleCount,
                usage);
        }
    }

    #endregion

    #region Color

    /// <summary>
    /// Converts a <see cref="Color"/> to a <see cref="Vector3"/> with normalized components in the HSV (Hue, Saturation, Value) colorspace
    /// — not to be confused with HSL/HSB (Hue, Saturation, Lightness/Brightness), see <see href="https://en.wikipedia.org/wiki/HSL_and_HSV">here</see>, for more information. —
    /// </summary>
    public static Vector3 ColorToHSV(Color color)
    {
        float max = MathF.Max(color.R, MathF.Max(color.G, color.B)) / 255f;
        float min = MathF.Min(color.R, MathF.Min(color.G, color.B)) / 255f;

        float hue = Main.rgbToHsl(color).X;
        float sat = (max == 0) ? 0f : 1f - (1f * min / max);
        float val = max;

        return new(hue, sat, val);
    }

    /// <summary>
    /// Converts a <see cref="Vector3"/> with normalized components in the HSV (Hue, Saturation, Value) colorspace
    /// — not to be confused with HSL/HSB (Hue, Saturation, Lightness/Brightness), see <see href="https://en.wikipedia.org/wiki/HSL_and_HSV">here</see>, for more information; —
    /// to a <see cref="Color"/>.
    /// </summary>
    public static Color HSVToColor(Vector3 hsv)
    {
        int hue = (int)(hsv.X * 360f);

        float num2 = hsv.Y * hsv.Z;
        float num3 = num2 * (1f - MathF.Abs(hue / 60f % 2f - 1f));
        float num4 = hsv.Z - num2;

        return hue switch
        {
            < 60 => new(num4 + num2, num4 + num3, num4),
            < 120 => new(num4 + num3, num4 + num2, num4),
            < 180 => new(num4, num4 + num2, num4 + num3),
            < 240 => new(num4, num4 + num3, num4 + num2),
            < 300 => new(num4 + num3, num4, num4 + num2),
            _ => new(num4 + num2, num4, num4 + num3)
        };
    }

    /// <inheritdoc cref="HSVToColor(Vector3)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color HSVToColor(float hue, float sat = 1f, float val = 1f) =>
        HSVToColor(new(hue, sat, val));

    #endregion

    #region UI

    /// <summary>
    /// Draws a slider similar to <see cref="RangeElement"/>'s, but without drawing the inner texture nor the dial.
    /// </summary>
    /// <param name="ratio">Value between 0-1 based on where the mouse is on the slider.</param>
    /// <param name="inner">The rectangle that can be used to draw the innermost texture; usually a gradient.</param>
    public static void DrawVanillaSlider(SpriteBatch spriteBatch, Color color, bool isHovering, out float ratio, out Rectangle destinationRectangle, out Rectangle inner)
    {
        Texture2D colorBar = TextureAssets.ColorBar.Value;
        Texture2D colorBarHighlight = TextureAssets.ColorHighlight.Value;

        Rectangle rectangle = new((int)IngameOptions.valuePosition.X, (int)IngameOptions.valuePosition.Y - (int)(colorBar.Height * .5f), colorBar.Width, colorBar.Height);
        destinationRectangle = rectangle;

        float x = rectangle.X + 5f;
        float y = rectangle.Y + 4f;

        spriteBatch.Draw(colorBar, rectangle, color);

        inner = new((int)x, (int)y, (int)SliderWidth + 2, 8);

        rectangle.Inflate(-5, 2);

        if (isHovering)
            spriteBatch.Draw(colorBarHighlight, destinationRectangle, Main.OurFavoriteColor);

        ratio = Saturate((Main.mouseX - rectangle.X) / (float)rectangle.Width);
    }

    public static void DrawSplitConfigPanel(SpriteBatch spriteBatch, Color color, Rectangle dims, int split = 15)
    {
        Texture2D texture = TextureAssets.SettingsPanel.Value;

            // Left/Right bars.
        spriteBatch.Draw(texture, new Rectangle(dims.X, dims.Y + 2, 2, dims.Height - 4), new(0, 2, 1, 1), color);
        spriteBatch.Draw(texture, new Rectangle(dims.X + dims.Width - 2, dims.Y + 2, 2, dims.Height - 4), new(0, 2, 1, 1), color);

            // Up/Down bars.
        spriteBatch.Draw(texture, new Rectangle(dims.X + 2, dims.Y, dims.Width - 4, 2), new(2, 0, 1, 1), color);
        spriteBatch.Draw(texture, new Rectangle(dims.X + 2, dims.Y + dims.Height - 2, dims.Width - 4, 2), new(2, 0, 1, 1), color);

            // Inner Panel.
        spriteBatch.Draw(texture, new Rectangle(dims.X + 2, dims.Y + 2, dims.Width - 4, split - 2), new(2, 2, 1, 1), color);
        spriteBatch.Draw(texture, new Rectangle(dims.X + 2, dims.Y + split, dims.Width - 4, dims.Height - split - 2), new(2, 16, 1, 1), color);
    }

    #endregion
}
