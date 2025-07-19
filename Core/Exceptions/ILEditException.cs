using MonoMod.Cil;
using System;
using System.IO;
using Terraria.ModLoader;
using ZensSky.Core.Utils;

namespace ZensSky.Core.Exceptions;

public class ILEditException : Exception
{
    public ILEditException(Mod mod, ILContext il, Exception? inner)
        : base($"\"{mod.Name}\" failed to IL edit method \"{il.Method.FullName}!\"" +
            $"\nA dump of the edited method has been created at: \"{Path.Combine(Logging.LogDir, "ILDumps", mod.Name)}.\"", inner) =>
        SafeDumpIL(mod, il);

    /// <summary>
    /// Safely dumps the information about the given <see cref="ILContext"/> to a file in Logs/ILDumps/{ModName}/{MethodName}.txt, now accounting for file length and shortening the file name if it exceeds 256.<br/>
    /// It may be useful to use a tool such as <see href="https://www.diffchecker.com/"/> to compare the IL before and after edits.
    /// </summary>
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

            // Here we limit the size of the file name as to not make poor windows cry.
        text = text.Truncate(250, ".txt");

        string text3 = Path.Combine(Logging.LogDir, "ILDumps", mod.Name, text);

        string? directoryName = Path.GetDirectoryName(text3);
        if (!Directory.Exists(directoryName) && directoryName is not null)
            Directory.CreateDirectory(directoryName);

        File.WriteAllText(text3, il.ToString());
        Logging.tML.Debug($"Dumped ILContext \"{il.Method.FullName}\" to \"{text3}\"");
    }
}
