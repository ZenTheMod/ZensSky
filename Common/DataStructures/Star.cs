using Microsoft.Xna.Framework.Graphics;
using System;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.GameContent;
using Terraria.Utilities;
using ZensSky.Core.Utils;

namespace ZensSky.Common.DataStructures;

public enum StarVisual : byte
{
    Vanilla,
    Diamond,
    FourPointed,
    OuterWilds,
    Random
}

    // Thanks to jupiter.ryo for early help with this.
/// <summary>
/// A simpler version of <see cref="Terraria.Star"/> that allows for multiple styles.
/// </summary>
public record struct Star
{
    #region Private Fields

    private static readonly Color LowestTemperature = new(255, 174, 132);
    private static readonly Color LowTemperature = new(255, 242, 238);
    private static readonly Color HighTemperature = new(236, 238, 255);
    private static readonly Color HighestTemperature = new(113, 135, 255);

    private const float MinScale = .3f;
    private const float MaxScale = 1.25f;
    private const float MaxTwinkle = 2f;
    private const int StarStyles = 4;
    private const float CircularRadius = 1200f;
    private const float LowTempThreshold = .4f;
    private const float HighTempThreshold = .6f;

    private const float VanillaStyleScale = .95f;

    private const float TwinkleTimeMultiplier = .35f;
    private const float TwinkleMinScale = .65f;

    private const float DiamondSize = .124f;
    private const float DiamondAlpha = .75f;

    private const float FlareSize = .14f;
    private const float FlareInnerSize = .03f;

    private const float CircleSize = .3f;
    private const float CircleAlpha = .67f;

    #endregion

    #region Public Properties

    public Vector2 Position { get; set; }

    public Color Color { get; set; }

    public float Scale { get; set; }

    public float Rotation { get; init; }

    public float Twinkle { get; init; }

    public int Style { get; init; }

    public bool Disabled { get; set; }

    #endregion

    #region Public Constructors

    public Star(UnifiedRandom rand)
    {
        Position = rand.NextUniformVector2Circular(CircularRadius);
        Color = GenerateColor(rand.NextFloat(1));
        Scale = rand.NextFloat(MinScale, MaxScale);
        Style = rand.Next(0, StarStyles);
        Rotation = rand.NextFloatDirection();
        Twinkle = rand.NextFloat(MaxTwinkle);
        Disabled = false;
    }

    #endregion

    #region Drawing

    public readonly void DrawVanilla(SpriteBatch spriteBatch, float alpha)
    {
        Texture2D texture = TextureAssets.Star[Style].Value;
        Vector2 origin = texture.Size() * .5f;

        Vector2 position = Position;

        Color color = Color * GetAlpha(alpha);

        float twinklePhase = Twinkle + (Main.GlobalTimeWrappedHourly * TwinkleTimeMultiplier);
        float twinkle = Utils.Remap(MathF.Sin(twinklePhase), -1, 1, TwinkleMinScale, 1f);

        float scale = Scale * VanillaStyleScale * twinkle;

        float rotation = (Main.GlobalTimeWrappedHourly * .1f * Twinkle) + Rotation;

        spriteBatch.Draw(texture, position, null, color, rotation, origin, scale, SpriteEffects.None, 0f);
    }

    public readonly void DrawDiamond(SpriteBatch spriteBatch, Texture2D texture, float alpha, Vector2 origin, float rotation)
    {
        Vector2 position = Position;

        Color color = Color * GetAlpha(alpha) * DiamondAlpha;
        color.A = 0;

        float twinklePhase = Twinkle + (Main.GlobalTimeWrappedHourly * TwinkleTimeMultiplier);
        float twinkle = Utils.Remap(MathF.Sin(twinklePhase), -1, 1, 0.8f, 1.2f);

        float scale = twinkle * Scale * DiamondSize;

        spriteBatch.Draw(texture, position, null, color, rotation, origin, scale, SpriteEffects.None, 0f);
    }

    public readonly void DrawFlare(SpriteBatch spriteBatch, Texture2D texture, float alpha, Vector2 origin, float rotation)
    {
        Vector2 position = Position;

        Color color = Color * GetAlpha(alpha);
        color.A = 0;

        float scale = Scale * FlareSize;

        spriteBatch.Draw(texture, position, null, color, rotation, origin, scale, SpriteEffects.None, 0f);

        Color white = Color.White * GetAlpha(alpha);
        color.A = 0;

        scale = Scale * FlareInnerSize;

        spriteBatch.Draw(texture, position, null, white, rotation, origin, scale, SpriteEffects.None, 0f);
    }

    public readonly void DrawCircle(SpriteBatch spriteBatch, Texture2D texture, float alpha, Vector2 origin, float rotation)
    {
        Vector2 position = Position;

        Color color = Color * GetAlpha(alpha) * CircleAlpha;
        color.A = 0;

        float scale = Scale * CircleSize;

        spriteBatch.Draw(texture, position, null, color, rotation, origin, scale, SpriteEffects.None, 0f);
    }

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Color GenerateColor(float temperature) =>
        temperature switch
        {
            <= LowTempThreshold => Color.Lerp(LowestTemperature, LowTemperature, Utils.Remap(temperature, 0f, LowTempThreshold, 0f, 1f)),
            <= HighTempThreshold => Color.Lerp(LowTemperature, HighTemperature, Utils.Remap(temperature, LowTempThreshold, HighTempThreshold, 0f, 1f)),
            _ => Color.Lerp(HighTemperature, HighestTemperature, Utils.Remap(temperature, HighTempThreshold, 1f, 0f, 1f))
        };

    public readonly float GetAlpha(float a) =>
        Utilities.Saturate(MathF.Pow(a + Scale, 3) * a);
}
