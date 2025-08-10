using Microsoft.Xna.Framework;
using System;
using Terraria;
using ZensSky.Common.Systems.Compat;

namespace ZensSky.Common.DataStructures;

public record struct WindParticle
{
    #region Private Fields

    private const int MaxOldPositions = 43;

    private const float LifeTimeIncrement = 0.004f;

    private const float SinLifeTimeFrequency = 7f;
    private const float SinGlobalTimeFrequency = .6f;

    private const float SinAmplitude = .1f;

    private const float LoopMin = 0.47f;
    private const float LoopMax = 0.53f;

    private const float Magnitude = 13f;

    #endregion

    #region Public Properties

    public required Vector2 Position { get; set; }
    public required Vector2[] OldPositions { get; init; }
    public required Vector2 Velocity { get; set; }
    public required bool ShouldLoop { get; init; }
    public required float LifeTime { get; set; }
    public required bool IsActive { get; set; }

    #endregion

    #region Updating

    public void Update()
    {
        float wind = Main.WindForVisuals;
        float increment = LifeTimeIncrement * MathF.Abs(wind);

        LifeTime += increment;
        if (LifeTime > 1f)
            IsActive = false;

        Vector2 newVelocity = new(wind, 
            MathF.Sin((LifeTime * SinLifeTimeFrequency + Main.GlobalTimeWrappedHourly) * SinGlobalTimeFrequency) * SinAmplitude);

        if (ShouldLoop)
        {
            float interpolator = Utils.Remap(LifeTime, LoopMin, LoopMax, 0f, 1f);
            newVelocity = newVelocity.RotatedBy(MathHelper.TwoPi * interpolator * -MathF.Sign(wind));
        }

        Velocity = newVelocity.SafeNormalize(Vector2.UnitY) * Magnitude * MathF.Abs(wind);

        Position += Velocity;

            // Update the old positions.
        for (int i = OldPositions.Length - 2; i >= 0; i--)
            OldPositions[i + 1] = OldPositions[i];

        OldPositions[0] = Position;
    }

    #endregion

    public static WindParticle CreateActive(Vector2 position, bool shouldLoop) => new()
    {
        Position = position,
        OldPositions = new Vector2[MaxOldPositions],
        Velocity = Vector2.Zero,
        ShouldLoop = shouldLoop,
        LifeTime = 0f,
        IsActive = true
    };
}
