using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.Utilities;
using ZensSky.Core.Utils;
using Terraria.ID;

namespace ZensSky.Common.DataStructures;

public record struct ShootingStar
{
    #region Private Fields

    private const int MaxOldPositions = 40;

    private const float LifeTimeIncrement = 0.007f;

    private const float MinVelocity = 3.1f;
    private const float MaxVelocity = 6.3f;

    private const float MinRotate = -.009f;
    private const float MaxRotate = .009f;

    private const float VelocityDegrade = .97f;

    private const float StarGameDistance = 4800f;
    private const float StarGameReflect = 10f;

    #endregion

    #region Public Properties

    public required Vector2 Position { get; set; }
    public required Vector2[] OldPositions { get; init; }
    public required Vector2 Velocity { get; set; }
    public required float Rotate { get; init; }
    public required float LifeTime { get; set; }
    public required bool IsActive { get; set; }
    public required bool Hit { get; set; }

    #endregion

    #region Updating

    public void Update()
    {
        LifeTime -= LifeTimeIncrement;
        if (LifeTime <= 0f)
            IsActive = false;

            // This is a really excessive way to lessen the velocity over time.
        float exponentialFade = 1f - MathF.Pow(2f, 10f * (1f - LifeTime - 1f));
        Velocity *= MathHelper.Lerp(1f, VelocityDegrade, exponentialFade);

            // Have the shooting star curve slightly
        Velocity = Velocity.RotatedBy(Rotate);

        Position += Velocity;

            // Update the old positions.
        for (int i = OldPositions.Length - 2; i >= 0; i--)
            OldPositions[i + 1] = OldPositions[i];
        OldPositions[0] = Position;
    }

    public void StarGameUpdate()
    {
        Update();

        if (Hit || Position.DistanceSQ(Utilities.MousePosition) >= StarGameDistance)
            return;

        Main.starsHit++;

        float magnitude = Velocity.Length();

        Velocity = Position - Utilities.MousePosition;
        Velocity = Vector2.Normalize(Velocity) * magnitude * StarGameReflect;

        Hit = true;

        SoundEngine.PlaySound(in SoundID.CoinPickup);
        SoundEngine.PlaySound(in SoundID.Meowmere);
    }

    #endregion

    public static ShootingStar CreateActive(Vector2 position, UnifiedRandom rand) => new()
    {
        Position = position,
        OldPositions = new Vector2[MaxOldPositions],
        Velocity = rand.NextVector2CircularEdge(1f, 1f) * rand.NextFloat(MinVelocity, MaxVelocity),
        Rotate = rand.NextFloat(MinRotate, MaxRotate),
        LifeTime = 1f,
        IsActive = true,
        Hit = false
    };
}
