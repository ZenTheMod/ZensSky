using System.Runtime.CompilerServices;
using Terraria;
using Terraria.Utilities;
using ZensSky.Common.Systems.Stars;
using ZensSky.Common.Utilities;

namespace ZensSky.Common.DataStructures;

public readonly record struct InteractableStar
{
    private static readonly Color LowestTemperature = new(255, 204, 152);
    private static readonly Color LowTemperature = new(255, 242, 238);
    private static readonly Color HighTemperature = new(236, 238, 255);
    private static readonly Color HighestTemperature = new(153, 185, 255);
    private static readonly Color Compressed = new(255, 96, 136);

    private const float MinTemperature = 4000f;
    private const float MaxTemperature = 30000f;
    private const float MinSize = 0.3f;
    private const float MaxSize = 1.2f;
    private const float MaxTwinkle = 2f;
    private const int MaxStarType = 4;
    private const float CircularRadius = 1200f;
    private const float LowTempThreshold = 0.4f;
    private const float HighTempThreshold = 0.6f;

    public required Vector2 Position { get; init; }
    public required float Temperature { get; init; }
    public required float BaseSize { get; init; }
    public required float Compression { get; init; }
    public required float Rotation { get; init; }
    public required float Twinkle { get; init; }
    public required int StarType { get; init; }

    public static InteractableStar CreateRandom(UnifiedRandom rand) => new()
    {
        Position = rand.NextUniformVector2Circular(CircularRadius),
        Temperature = rand.Next((int)MinTemperature, (int)MaxTemperature),
        BaseSize = rand.NextFloat(MinSize, MaxSize),
        StarType = rand.Next(0, MaxStarType),
        Rotation = rand.NextFloatDirection(),
        Twinkle = rand.NextFloat(MaxTwinkle),
        Compression = 0f
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Color GetColor()
    {
        float interpolate = Temperature / MaxTemperature;
        Color baseColor = interpolate switch
        {
            <= LowTempThreshold => Color.Lerp(LowestTemperature, LowTemperature, Utils.Remap(interpolate, 0f, LowTempThreshold, 0f, 1f)),
            <= HighTempThreshold => Color.Lerp(LowTemperature, HighTemperature, Utils.Remap(interpolate, LowTempThreshold, HighTempThreshold, 0f, 1f)),
            _ => Color.Lerp(HighTemperature, HighestTemperature, Utils.Remap(interpolate, HighTempThreshold, 1f, 0f, 1f))
        };

        return Color.Lerp(baseColor, Compressed, Compression);
    }

    public Vector2 GetRotatedPosition() => Position.RotatedBy(StarSystem.StarRotation);
}
