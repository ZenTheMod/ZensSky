using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Utilities;

namespace ZensSky.Core.Utils;

public static partial class Utilities
{
    #region UnifiedRandom

    /// <summary>
    /// Generate a <see cref="Vector2"/> uniformly in a circle with <paramref name="radius"/> as the radius.
    /// </summary>
    public static Vector2 NextUniformVector2Circular(this UnifiedRandom rand, float radius)
    {
        float a = rand.NextFloat() * 2 * MathHelper.Pi;
        float r = radius * MathF.Sqrt(rand.NextFloat());

        return new Vector2(r * MathF.Cos(a), r * MathF.Sin(a));
    }

    #endregion

    /// <returns><paramref name="value"/> between 0-1.</returns>
    public static float Saturate(float value) => MathHelper.Clamp(value, 0, 1);

    #region Rectangle

    public static Vector2 Position(this Rectangle rectangle) =>
         rectangle.TopLeft();

    public static bool Contains(this Rectangle rectangle, Vector2 position) =>
        rectangle.Contains((int)position.X, (int)position.Y);

    #endregion
}
