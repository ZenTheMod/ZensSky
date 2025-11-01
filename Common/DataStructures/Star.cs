using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.GameContent;
using Terraria.Utilities;
using ZensSky.Core.Utils;

namespace ZensSky.Common.DataStructures;

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

    private const float MaxTwinkle = MathHelper.TwoPi;

    private const int StarStyles = 4;

    private const float LowTempThreshold = .4f;
    private const float HighTempThreshold = .6f;

    private const float TwinkleTimeMultiplier = 1.45f;

    private const float VanillaScale = .95f;
    private const float VanillaTwinkleMin = .75f;
    private const float VanillaTwinkleMax = 1.03f;

    private const float DiamondSize = .124f;
    private const float DiamondAlpha = .75f;
    private const float DiamondTwinkleMin = .8f;
    private const float DiamondTwinkleMax = 1.20f;

    private const float FlareSize = .14f;
    private const float FlareInnerSize = .03f;
    private const float FlareTwinkleMin = .85f;
    private const float FlareTwinkleMax = 1.45f;

    private const float CircleSize = .3f;
    private const float CircleAlpha = .67f;
    private const float CircleTwinkleMin = .85f;
    private const float CircleTwinkleMax = 1.3f;

    #endregion

    #region Public Properties

    public Vector2 Position { get; set; }

    public Color Color { get; set; }

    public float Scale { get; set; }

    public float Rotation { get; init; }

    public float TwinklePhase { get; init; }

    public int Style { get; init; }

    public bool IsActive { get; set; } = true;

    #endregion

    #region Public Constructors

    public Star(UnifiedRandom rand, float circularRadius)
    {
        Position = rand.NextUniformVector2Circular(circularRadius);
        Color = GenerateColor(rand.NextFloat(1));
        Scale = rand.NextFloat(MinScale, MaxScale);
        Style = rand.Next(0, StarStyles);
        Rotation = rand.NextFloatDirection();
        TwinklePhase = rand.NextFloat(MaxTwinkle);
    }

    public Star(UnifiedRandom rand, Rectangle rectangle)
    {
        Position = rand.NextVector2FromRectangle(rectangle);
        Color = GenerateColor(rand.NextFloat(1));
        Scale = rand.NextFloat(MinScale, MaxScale);
        Style = rand.Next(0, StarStyles);
        Rotation = rand.NextFloatDirection();
        TwinklePhase = rand.NextFloat(MaxTwinkle);
    }

    #endregion

    #region Drawing

    public readonly void DrawVanilla(SpriteBatch spriteBatch, float alpha)
    {
        Texture2D texture = TextureAssets.Star[Style].Value;
        Vector2 origin = texture.Size() * .5f;

        Vector2 position = Position;

        Color color = Color * GetAlpha(alpha);

        float twinkle = TwinkleScale(VanillaTwinkleMin, VanillaTwinkleMax);

        float scale = Scale * VanillaScale * twinkle;

        float rotation = (Main.GlobalTimeWrappedHourly * .1f * TwinklePhase) + Rotation;

        spriteBatch.Draw(texture, position, null, color, rotation, origin, scale, SpriteEffects.None, 0f);
    }

    public readonly void DrawDiamond(SpriteBatch spriteBatch, Texture2D texture, float alpha, Vector2 origin, float rotation)
    {
        Vector2 position = Position;

        Color color = Color * GetAlpha(alpha) * DiamondAlpha;
        color.A = 0;

        float twinkle = TwinkleScale(DiamondTwinkleMin, DiamondTwinkleMax);

        float scale = twinkle * Scale * DiamondSize;

        spriteBatch.Draw(texture, position, null, color, rotation, origin, scale, SpriteEffects.None, 0f);
    }

    public readonly void DrawFlare(SpriteBatch spriteBatch, Texture2D texture, float alpha, Vector2 origin, float rotation)
    {
        Vector2 position = Position;

        Color color = Color * GetAlpha(alpha);
        color.A = 0;

        float twinkle = TwinkleScale(FlareTwinkleMin, FlareTwinkleMax);

        float scale = twinkle * Scale * FlareSize;

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

        float twinkle = TwinkleScale(CircleTwinkleMin, CircleTwinkleMax);

        float scale = twinkle * Scale * CircleSize;

        spriteBatch.Draw(texture, position, null, color, rotation, origin, scale, SpriteEffects.None, 0f);
    }

    #endregion

    #region Private Methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Color GenerateColor(float temperature) =>
        temperature switch
        {
            <= LowTempThreshold => Color.Lerp(LowestTemperature, LowTemperature, Utils.Remap(temperature, 0f, LowTempThreshold, 0f, 1f)),
            <= HighTempThreshold => Color.Lerp(LowTemperature, HighTemperature, Utils.Remap(temperature, LowTempThreshold, HighTempThreshold, 0f, 1f)),
            _ => Color.Lerp(HighTemperature, HighestTemperature, Utils.Remap(temperature, HighTempThreshold, 1f, 0f, 1f))
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly float TwinkleScale(float min, float max) =>
        Utils.Remap(MathF.Sin(TwinklePhase + (Main.GlobalTimeWrappedHourly * TwinkleTimeMultiplier)), -1, 1, min, max);

    #endregion

    #region Public Methods

    public readonly float GetAlpha(float a) =>
        Utilities.Saturate(MathF.Pow(a + Scale, 3) * a);

    #endregion
}
