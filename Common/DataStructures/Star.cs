﻿using System.Runtime.CompilerServices;
using Terraria;
using Terraria.Utilities;
using ZensSky.Common.Utilities;

namespace ZensSky.Common.DataStructures;

public enum SupernovaProgress : byte
{
    None = 0,
    Shrinking = 1,
    Exploding = 2,
    Regenerating = 3
}

    // Thanks to jupiter.ryo for early help with this.
/// <summary>
/// A simpler version of <see cref="Terraria.Star"/> that provides extra supernova functionality with <see cref="SupernovaTimer"/> and <see cref="SupernovaProgress"/>.
/// </summary>
public record struct Star
{
    #region Private Fields

    private static readonly Color LowestTemperature = new(255, 204, 152);
    private static readonly Color LowTemperature = new(255, 242, 238);
    private static readonly Color HighTemperature = new(236, 238, 255);
    private static readonly Color HighestTemperature = new(153, 185, 255);
    private static readonly Color Compressed = Color.White;

    private const float MinSize = 0.3f;
    private const float MaxSize = 1.2f;
    private const float MaxTwinkle = 2f;
    private const int MaxStarType = 4;
    private const float CircularRadius = 1200f;
    private const float LowTempThreshold = 0.4f;
    private const float HighTempThreshold = 0.6f;

    #endregion

    #region Public Properties

    public required Vector2 Position { get; init; }

    public required Color Color { get; init; }

    public required float BaseSize { get; init; }

    public required float Rotation { get; init; }

    public required float Twinkle { get; init; }

    public required int StarType { get; init; }

    public float SupernovaTimer { get; set; }
    public SupernovaProgress SupernovaProgress { get; set; }

    #endregion

    public static Star CreateRandom(UnifiedRandom rand) => new()
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
    public readonly Color GetColor() => Color.Lerp(Color, Compressed, SupernovaTimer);
}
