using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Utilities;

namespace ZensSky.Common.DataStructures;

public record struct ShootingStar
{
    #region Private Fields

    private const int MaxOldPositions = 40;

    private const float LifeTimeIncrement = 0.007f;

    private const float MinVelocity = 2f;
    private const float MaxVelocity = 4.5f;

    private const float MinSize = 0.7f;
    private const float MaxSize = 1.1f;

    private const float VelocityDegrade = .97f;

    #endregion

    #region Public Properties

    public required Vector2 Position { get; set; }
    public required Vector2[] OldPositions { get; init; }
    public required Vector2 Velocity { get; set; }
    public required float Size { get; init; }
    public required float LifeTime { get; set; }
    public required bool IsActive { get; set; }

    #endregion

    #region Updating

    public void Update()
    {
        LifeTime -= LifeTimeIncrement;
        if (LifeTime <= 0f)
            IsActive = false;

        float exponentialFade = 1f - MathF.Pow(2f, 10f * (1f - LifeTime - 1f));

        Velocity *= MathHelper.Lerp(1f, VelocityDegrade, exponentialFade);

        Position += Velocity;

            // Update the old positions.
        for (int i = OldPositions.Length - 2; i >= 0; i--)
            OldPositions[i + 1] = OldPositions[i];
        OldPositions[0] = Position;
    }

    #endregion

    public static ShootingStar CreateActive(Vector2 position, UnifiedRandom rand) => new()
    {
        Position = position,
        OldPositions = new Vector2[MaxOldPositions],
        Velocity = rand.NextVector2CircularEdge(1f, 1f) * rand.NextFloat(MinVelocity, MaxVelocity),
        Size = rand.NextFloat(MinSize, MaxSize),
        LifeTime = 1f,
        IsActive = true
    };
}
