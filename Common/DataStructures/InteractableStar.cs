using System.Runtime.CompilerServices;
using Terraria;
using Terraria.Utilities;
using ZensSky.Common.Systems.Stars;
using ZensSky.Common.Utilities;
using ZensSky.Common.Config;

namespace ZensSky.Common.DataStructures;

public enum SupernovaProgress : byte
{
    None = 0,
    Shrinking = 1,
    Exploding = 2,
    Regenerating = 3
}

public record struct InteractableStar
{
    #region Private Fields

    private static readonly Color LowestTemperature = new(255, 204, 152);
    private static readonly Color LowTemperature = new(255, 242, 238);
    private static readonly Color HighTemperature = new(236, 238, 255);
    private static readonly Color HighestTemperature = new(153, 185, 255);
    private static readonly Color Compressed = new(255, 96, 136);

    private const float MinSize = 0.3f;
    private const float MaxSize = 1.2f;
    private const float MaxTwinkle = 2f;
    private const int MaxStarType = 4;
    private const float CircularRadius = 1200f;
    private const float LowTempThreshold = 0.4f;
    private const float HighTempThreshold = 0.6f;

    #endregion

    /// <summary>
    /// The position of the star, is not relative to the top left of the screen.
    /// </summary>
    public required Vector2 Position { get; init; }

    /// <summary>
    /// The color of the star, by default uses <see cref="GenerateColor"/>.
    /// </summary>
    public required Color Color { get; init; }

    /// <summary>
    /// The base scale of the star despite compression.
    /// </summary>
    public required float BaseSize { get; init; }

    public required float Rotation { get; init; }

    /// <summary>
    /// How frequently the star 'twinkles'.
    /// </summary>
    public required float Twinkle { get; init; }

    /// <summary>
    /// Which one of vanillas star textures the star uses when drawn while <see cref="SkyConfig.VanillaStyleStars"/> is true.
    /// </summary>
    public required int StarType { get; init; }

    /// <summary>
    /// Timer used for controlling the supernova visual.
    /// </summary>
    public float SupernovaTimer { get; set; }

    public SupernovaProgress SupernovaProgress { get; set; }

    public static InteractableStar CreateRandom(UnifiedRandom rand) => new()
    {
        Position = rand.NextUniformVector2Circular(CircularRadius),
        Color = GenerateColor(rand.NextFloat(1)),
        BaseSize = rand.NextFloat(MinSize, MaxSize),
        StarType = rand.Next(0, MaxStarType),
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Color GetColor() => SupernovaProgress == SupernovaProgress.Shrinking ? Color.Lerp(Color, Compressed, SupernovaTimer) : Color;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Vector2 GetRotatedPosition() => Position.RotatedBy(StarSystem.StarRotation);
}
