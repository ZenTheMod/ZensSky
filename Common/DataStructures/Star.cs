using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.GameContent;
using Terraria.Utilities;
using ZensSky.Common.Registries;
using ZensSky.Common.Systems.Stars;
using ZensSky.Common.Utilities;

namespace ZensSky.Common.DataStructures;

public enum SupernovaProgress : byte
{
    None = 0,
    Shrinking = 1,
    Exploding = 2,
    Regenerating = 3
}

public enum StarVisual : byte
{
    Vanilla = 0,
    Diamond = 1,
    FourPointed = 2,
    OuterWilds = 3,
    Random = 4
}

    // Thanks to jupiter.ryo for early help with this.
/// <summary>
/// A simpler version of <see cref="Terraria.Star"/> that provides extra supernova functionality with <see cref="SupernovaTimer"/> and <see cref="SupernovaProgress"/>.
/// </summary>
public record struct Star
{
    #region Private Fields

    private static readonly Color LowestTemperature = new(255, 174, 132);
    private static readonly Color LowTemperature = new(255, 242, 238);
    private static readonly Color HighTemperature = new(236, 238, 255);
    private static readonly Color HighestTemperature = new(113, 135, 255);
    private static readonly Color Compressed = Color.White;

    private const float MinSize = 0.3f;
    private const float MaxSize = 1.2f;
    private const float MaxTwinkle = 2f;
    private const int VanillaStarStyles = 4;
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

    public required Vector2 Position { get; set; }

    public required Color BaseColor { get; init; }

    public required float BaseSize { get; init; }

    public required float Rotation { get; init; }

    public required float Twinkle { get; init; }

    public required int VanillaStyle { get; init; }

    public float SupernovaTimer { get; set; }

    public SupernovaProgress SupernovaProgress { get; set; }

    public readonly Color Color =>
        Color.Lerp(BaseColor, Compressed, SupernovaTimer);

    public readonly float Scale =>
        BaseSize * (1 - MathF.Pow(SupernovaTimer, 3));

    #endregion

    #region Drawing

    public readonly void DrawVanilla(SpriteBatch spriteBatch, float alpha)
    {
        if (SupernovaProgress == SupernovaProgress.Exploding)
            return;

        Texture2D texture = TextureAssets.Star[VanillaStyle].Value;
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
        if (SupernovaProgress == SupernovaProgress.Exploding)
            return;

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
        if (SupernovaProgress == SupernovaProgress.Exploding)
            return;

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
        if (SupernovaProgress == SupernovaProgress.Exploding)
            return;

        Vector2 position = Position;

        Color color = Color * GetAlpha(alpha) * CircleAlpha;
        color.A = 0;

        float scale = Scale * CircleSize;

        spriteBatch.Draw(texture, position, null, color, rotation, origin, scale, SpriteEffects.None, 0f);
    }

    #endregion

    public static Star CreateRandom(UnifiedRandom rand) => new()
    {
        Position = rand.NextUniformVector2Circular(CircularRadius),
        BaseColor = GenerateColor(rand.NextFloat(1)),
        BaseSize = rand.NextFloat(MinSize, MaxSize),
        VanillaStyle = rand.Next(0, VanillaStarStyles),
        Rotation = rand.NextFloatDirection(),
        Twinkle = rand.NextFloat(MaxTwinkle)
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Color GenerateColor(float temperature)
    {
        return temperature switch
        {
            <= LowTempThreshold => Color.Lerp(LowestTemperature, LowTemperature, Utils.Remap(temperature, 0f, LowTempThreshold, 0f, 1f)),
            <= HighTempThreshold => Color.Lerp(LowTemperature, HighTemperature, Utils.Remap(temperature, LowTempThreshold, HighTempThreshold, 0f, 1f)),
            _ => Color.Lerp(HighTemperature, HighestTemperature, Utils.Remap(temperature, HighTempThreshold, 1f, 0f, 1f))
        };
    }

    public readonly float GetAlpha(float a) =>
        MiscUtils.Saturate(MathF.Pow(a + MathF.Pow(SupernovaTimer, 3) + BaseSize, 2) * a);
}
