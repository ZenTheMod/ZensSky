using Microsoft.Xna.Framework;
using MonoMod.Cil;
using System;
using System.IO;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace ZensSky.Common.Utilities;

public static class MiscUtils
{
    #region Public Properties

    public static Rectangle ScreenDimensions => new(0, 0, Main.screenWidth, Main.screenHeight);

    public static Vector2 ScreenSize => new(Main.screenWidth, Main.screenHeight);

    public static Vector2 HalfScreenSize => ScreenSize * 0.5f;

    public static Vector2 MousePosition => new(PlayerInput.MouseX, PlayerInput.MouseY);

    #endregion

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
}
