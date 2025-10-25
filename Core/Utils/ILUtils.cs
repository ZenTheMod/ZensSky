using MonoMod.Cil;
using System;

namespace ZensSky.Core.Utils;

public static partial class Utilities
{
    public static void EmitCall<T>(this ILCursor c, T action) where T : Delegate =>
        c.EmitCall(action.Method);
}
