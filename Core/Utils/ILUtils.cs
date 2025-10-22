using MonoMod.Cil;
using System;

namespace ZensSky.Core.Utils;

public static partial class Utilities
{
    public static void EmitCall<T>(this ILCursor p, T action) where T : Delegate =>
        p.EmitCall(action.Method);
}
