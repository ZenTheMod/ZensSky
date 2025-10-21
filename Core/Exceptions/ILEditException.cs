using MonoMod.Cil;
using System;
using System.IO;
using Terraria.ModLoader;

namespace ZensSky.Core.Exceptions;

public class ILEditException : Exception
{
    public ILEditException(Mod mod, ILContext il, Exception? inner)
        : base($"\"{mod.Name}\" failed to IL edit method \"{il.Method.FullName}!\"" +
            $"\nA dump of the edited method has been created at: \"{Path.Combine(Logging.LogDir, "ILDumps", mod.Name)}.\"", inner) =>
        MonoModHooks.DumpIL(mod, il);
}
