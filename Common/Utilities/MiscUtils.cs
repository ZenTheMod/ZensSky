using Microsoft.Xna.Framework;
using MonoMod.Cil;
using System;
using System.IO;
using Terraria;
using Terraria.GameInput;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.Utilities;

namespace ZensSky.Common.Utilities;

public static class MiscUtils
{
    #region Public Properties

    public static Rectangle ScreenDimensions => new(0, 0, Main.screenWidth, Main.screenHeight);

    public static Vector2 ScreenSize => new(Main.screenWidth, Main.screenHeight);

    public static Vector2 HalfScreenSize => ScreenSize * 0.5f;

    public static Vector2 MousePosition => new(PlayerInput.MouseX, PlayerInput.MouseY);

    public static Vector2 UIMousePosition => UserInterface.ActiveInstance.MousePosition;

    #endregion

    #region RNG

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

    #endregion

    #region IL

    /// <summary>
    /// Safely umps the information about the given ILContext to a file in Logs/ILDumps/{ModName}/{Method Name}.txt, now accounting for file length and shortening it if it exceeds 255.
    /// It may be useful to use a tool such as https://www.diffchecker.com/ to compare the IL before and after edits.
    /// </summary>
    /// <param name="mod"></param>
    /// <param name="il"></param>
    public static void SafeDumpIL(Mod mod, ILContext il)
    {
        string text = il.Method.FullName.Replace(':', '_').Replace('<', '[').Replace('>', ']');
        if (text.Contains('?'))
        {
            string text2 = text;
            int num = text.LastIndexOf('?') + 1;
            text = text2[num..];
        }

        text = string.Join("_", text.Split(Path.GetInvalidFileNameChars()));

            // Here we limit the size of the string as to not make poor windows cry.
        text = text[..254];

        string text3 = Path.Combine(Logging.LogDir, "ILDumps", mod.Name, text + ".txt");
        string? directoryName = Path.GetDirectoryName(text3);
        if (!Directory.Exists(directoryName) && directoryName is not null)
            Directory.CreateDirectory(directoryName);

        File.WriteAllText(text3, il.ToString());
        Logging.tML.Debug($"Dumped ILContext \"{il.Method.FullName}\" to \"{text3}\"");
    }

    #endregion

    /// <summary>
    /// Safely invokes <paramref name="action"/> with <see cref="Main.QueueMainThreadAction"/> if it is ran on a client; otherwise it is invoked normally.<br/>
    /// — This is intended to be used for the application of IL edits/Detours;<br/>
    /// and is irrelevant for client-sided Mods or classes using <see cref="AutoloadAttribute"/> with <see cref="AutoloadAttribute.Side"/> set to <see cref="ModSide.Client"/> —<br/><br/>
    /// On servers with no clients connected <see cref="Main.QueueMainThreadAction"/> is not ran.<br/>
    /// The usage of <see cref="Main.QueueMainThreadAction"/> is to prevent a recent obscure MonoMod race condition issue.<br/>
    /// This seems to be the safest as it avoids hot paths like <see cref="Main.DoUpdate"/> and <see cref="Main.DoDraw"/>.<br/><br/>
    /// <code>
    /// System.ArgumentException: Referenced cell no longer exists (Parameter 'cellRef')
    /// </code>
    /// </summary>
    /// <param name="action"></param>
    public static void SafeMainThreadAction(Action action)
    {
        if (Main.dedServ)
            action?.Invoke();
        else
            Main.QueueMainThreadAction(() => action?.Invoke());
    }

    /// <summary>
    /// Shorthand for <c>MathHelper.Clamp(<paramref name="value"/>, 0, 1)</c>.
    /// </summary>
    /// <param name="value"></param>
    /// <returns><paramref name="value"/> between 0-1.</returns>
    public static float Saturate(float value) => MathHelper.Clamp(value, 0, 1);

    #region Lang

    /// <summary>
    /// Retrieves the text value for a specified localization key — but with glyph support via <see cref="Lang.SupportGlyphs"/>, allowing the use of <c>&lt;left&gt;</c> and <c>&lt;right&gt;</c> —. <br/>
    /// The text returned will be for the currently selected language.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public static string GetTextValueWithGlyphs(string key) =>
        Lang.SupportGlyphs(Language.GetTextValue(key));

    #endregion

    #region Triangles

    public static float Sign(Vector2 p1, Vector2 p2, Vector2 p3) =>
        (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);

    public static bool IsPointInTriangle(Vector2 point, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        float d1 = Sign(point, p1, p2);
        float d2 = Sign(point, p2, p3);
        float d3 = Sign(point, p3, p1);
        bool hasNegative = d1 < 0f || d2 < 0f || d3 < 0f;
        bool hasPositive = d1 > 0f || d2 > 0f || d3 > 0f;
        return !(hasNegative && hasPositive);
    }

    /// <summary>
    /// </summary>
    /// <param name="point"></param>
    /// <param name="points"></param>
    /// <returns><see cref="true"/> if <c>points.Length >= 3</c> and <paramref name="point"/> is inside of the triangle created by <paramref name="points"/>.</returns>
    public static bool IsPointInTriangle(Vector2 point, Vector2[] points) =>
        points.Length >= 3 && IsPointInTriangle(point, points[0], points[1], points[2]);

    public static Vector2 Size(this CalculatedStyle dims) =>
        new(dims.Width, dims.Height);

    public static Vector2 ClosestPointOnTriangle(Vector2 point, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        if (IsPointInTriangle(point, p1, p2, p3))
            return point;

        Vector2 c1 = point.ClosestPointOnLine(p1, p2);
        Vector2 c2 = point.ClosestPointOnLine(p2, p3);
        Vector2 c3 = point.ClosestPointOnLine(p3, p1);

        float mag1 = (point - c1).LengthSquared();
        float mag2 = (point - c2).LengthSquared();
        float mag3 = (point - c3).LengthSquared();

        float min = Math.Min(mag1, mag2);
        min = Math.Min(min, mag3);

        if (min == mag1)
            return c1;
        else if (min == mag2)
            return c2;
        return c3;
    }

    /// <summary>
    /// Clamps a point to the bounds of a triangle created by <paramref name="points"/>.
    /// </summary>
    /// <param name="point"></param>
    /// <param name="points"></param>
    /// <returns>The closest point on the triangle — returns <paramref name="point"/> if its already inside the triangle —, and <see cref="Vector2.Zero"/> if <c>points.Length &lt; 3</c></returns>
    public static Vector2 ClosestPointOnTriangle(Vector2 point, Vector2[] points) =>
        points.Length >= 3 ? ClosestPointOnTriangle(point, points[0], points[1], points[2]) : Vector2.Zero;

    /// <summary>
    /// Iterpolates between <paramref name="colors"/> — using cartesian coordinates — based on <paramref name="point"/> and the triangle formed by <paramref name="points"/>.
    /// </summary>
    /// <param name="point"></param>
    /// <param name="points"></param>
    /// <param name="colors"></param>
    /// <returns>Resulting color after interpolation, <see cref="Color.Transparent"/> if <c>points.Length  &lt; 3 || colors.Length &lt; 3</c></returns>
    public static Color LerpTriangle(Vector2 point, Vector2[] points, Color[] colors)
    {
        if (points.Length < 3 || colors.Length < 3)
            return Color.Transparent;

        float[] w = CartesianToBarycentric(point, points);

        Vector4 color = ((colors[0].ToVector4() * w[0]) + (colors[1].ToVector4() * w[1]) + (colors[2].ToVector4() * w[2])) / (w[0] + w[1] + w[2]);

        return new(color);
    }

    public static float[] CartesianToBarycentric(Vector2 point, Vector2[] points) 
    {
        Vector2 p1 = points[0];
        Vector2 p2 = points[1];
        Vector2 p3 = points[2];

        float y2y3 = p2.Y - p3.Y;
        float x3x2 = p3.X - p2.X;
        float x1x3 = p1.X - p3.X;
        float y1y3 = p1.Y - p3.Y;
        float y3y1 = p3.Y - p1.Y;
        float xx3 = point.X - p3.X;
        float yy3 = point.Y - p3.Y;

        float d = y2y3 * x1x3 + x3x2 * y1y3;
        float lambda1 = (y2y3 * xx3 + x3x2 * yy3) / d;
        float lambda2 = (y3y1 * xx3 + x1x3 * yy3) / d;

        return [lambda1, lambda2, 1 - lambda1 - lambda2];
    }

    #endregion
}
