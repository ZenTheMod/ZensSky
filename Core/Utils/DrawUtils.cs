using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader.Config.UI;
using Terraria.UI.Chat;
using static ReLogic.Graphics.DynamicSpriteFont;
using static System.Net.Mime.MediaTypeNames;

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

    #region Text

    public static void SlowDrawStringWithShadow(this SpriteBatch spriteBatch,
        DynamicSpriteFont font,
        string text,
        Vector2 position,
        Color color,
        Vector2 origin,
        Vector2 scale,
        out int hoveredChar,
        bool drawBlinker = false,
        int blinkerIndex = -1)
    {
        bool first = true;
        float lastKerning = 0f;

        hoveredChar = 0;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            spriteBatch.DrawStringWithShadow(font, c.ToString(), position, color, Color.Black, 0f, origin, scale);

            if (drawBlinker &&
                i == blinkerIndex)
            {
                Vector2 blinkerPosition = new(position.X - (2f * scale.X), position.Y);

                spriteBatch.DrawStringWithShadow(font, "|", blinkerPosition, color, Color.Black, 0f, origin, scale);
            }

            Vector2 charSize = font.MeasureChar(c, first, lastKerning, out lastKerning);

            if (MousePosition.X >= position.X && MousePosition.X <= position.X + charSize.X)
                hoveredChar = MousePosition.X >= position.X + (charSize.X * .5f) ? i + 1 : i;

            position.X += font.MeasureChar(c, first, lastKerning, out lastKerning).X;
            first = false;
        }

        if (MousePosition.X >= position.X)
            hoveredChar = text.Length;

        if (drawBlinker &&
            blinkerIndex >= text.Length)
        {
            Vector2 blinkerPosition = new(position.X - (2f * scale.X), position.Y);

            spriteBatch.DrawStringWithShadow(font, "|", blinkerPosition, color, Color.Black, 0f, origin, scale);
        }
    }

    public static void DrawStringWithShadow(this SpriteBatch spriteBatch,
        DynamicSpriteFont font,
        string text,
        Vector2 position,
        Color color,
        Color shadowColor,
        float rotation,
        Vector2 origin,
        Vector2 scale,
        float spread = 2f)
    {
        for (int i = 0; i < ChatManager.ShadowDirections.Length; i++)
            spriteBatch.DrawString(font, text, position + ChatManager.ShadowDirections[i] * spread, shadowColor, rotation, origin, scale, SpriteEffects.None, 0f);

        spriteBatch.DrawString(font, text, position, color, rotation, origin, scale, SpriteEffects.None, 0f);
    }

    #endregion
}
