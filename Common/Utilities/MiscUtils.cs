using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Utilities;

namespace ZensSky.Common.Utilities;

public static class MiscUtils
{
    public static Rectangle ScreenDimensions => new(0, 0, Main.screenWidth, Main.screenHeight);

    /// <summary>
    /// Shorthand for the screens size as a <see cref="Vector2"/>.
    /// </summary>
    public static Vector2 ScreenSize => new(Main.screenWidth, Main.screenHeight);

    /// <summary>
    /// Shorthand for half the screens size as a <see cref="Vector2"/>.
    /// </summary>
    public static Vector2 HalfScreenSize => ScreenSize * 0.5f;

    /// <summary>
    /// Generate a <see cref="Vector2"/> uniformly in a circle with <paramref name="radius"/> as the radius.
    /// </summary>
    /// <param name="rand"></param>
    /// <param name="radius"></param>
    /// <returns></returns>
    public static Vector2 NextUniformVector2Circular(this UnifiedRandom rand, float radius)
    {
        float a = rand.NextFloat() * 2 * MathHelper.Pi;
        float r = radius * MathF.Sqrt(rand.NextFloat());

        return new Vector2(r * MathF.Cos(a), r * MathF.Sin(a));
    }
}
