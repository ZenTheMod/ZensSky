using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace ZensSky.Common.DataStructures;

public record struct WindParticle
{
    #region Private Fields

    private const int MaxOldPositions = 12;

    private const float LifeTimeIncrement = 0.01f;
    private const float MinWind = 0.001f;

    private const float SinFrequency = 0.05f;

    private const float LoopMin = 0.4f;
    private const float LoopMax = 0.6f;

    private const float Magnitude = 50f;

    #endregion

    public required Vector2 Position { get; set; }
    public required Vector2[] OldPositions { get; init; }
    public required Vector2 Velocity { get; set; }
    public required bool ShouldLoop { get; init; }
    public required float LifeTime { get; set; }
    public required bool IsActive { get; set; }

    public void Update()
    {
        float wind = Main.WindForVisuals;
        float increment = 0.005f * MathF.Abs(wind);

        LifeTime += increment;
        if (LifeTime > 1f)
            IsActive = false;

        Vector2 newVelocity = new(wind, MathF.Sin(((LifeTime * 7) + Main.GlobalTimeWrappedHourly) * .6f) * .1f);

        if (ShouldLoop)
        {
            float interpolator = Utils.Remap(LifeTime, 0.45f, 0.55f, 0f, 1f);
            newVelocity = newVelocity.RotatedBy(MathHelper.TwoPi * interpolator * -MathF.Sign(wind));
        }

        Velocity = newVelocity.SafeNormalize(Vector2.UnitY) * 12f * MathF.Abs(wind);
        Position += Velocity;

            // Update the old positions.
        for (int i = OldPositions.Length - 2; i >= 0; i--)
            OldPositions[i + 1] = OldPositions[i];
        OldPositions[0] = Position;
    }

    public static WindParticle CreateActive(Vector2 position, bool shouldLoop) => new()
    {
        Position = position,
        OldPositions = new Vector2[32],
        Velocity = Vector2.Zero,
        ShouldLoop = shouldLoop,
        LifeTime = 0f,
        IsActive = true
    };
}
