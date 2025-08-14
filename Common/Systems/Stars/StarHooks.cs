using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using System.Runtime.CompilerServices;
using ZensSky.Core.Systems.ModCall;

namespace ZensSky.Common.Systems.Stars;

public static class StarHooks
{
    #region Public Hooks

    public delegate bool hook_PreDrawStars(SpriteBatch spriteBatch, ref float alpha, ref Matrix transform);

    [method: ModCall] // add_PreDrawStars, remove_PreDrawStars.
    public static event hook_PreDrawStars? PreDrawStars;

    public delegate void hook_PostDrawStars(SpriteBatch spriteBatch, float alpha, Matrix transform);

    [method: ModCall] // add_PostDrawStars, remove_PostDrawStars.
    public static event hook_PostDrawStars? PostDrawStars;

    #endregion

    #region Public Methods

        // Methods below are mainly included for Mod.Call support.
    [ModCall]
    public static void AddPreDrawStars(hook_PreDrawStars preDraw) =>
        PreDrawStars += preDraw;

    [ModCall]
    public static void AddPostDrawStars(hook_PostDrawStars postDraw) =>
        PostDrawStars += postDraw;

    [ModCall("PreDrawStars")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InvokePreDrawStars(SpriteBatch spriteBatch, ref float alpha, ref Matrix transform)
    {
        bool ret = true;

        if (PreDrawStars is null)
            return true;

        foreach (hook_PreDrawStars handler in
            PreDrawStars.GetInvocationList().Select(h => (hook_PreDrawStars)h))
            ret &= handler(spriteBatch, ref alpha, ref transform);

        return ret;
    }

    [ModCall("PostDrawStars")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InvokePostDrawStars(SpriteBatch spriteBatch, float alpha, Matrix transform) =>
        PostDrawStars?.Invoke(spriteBatch, alpha, transform);

    public static void Clear()
    {
        PreDrawStars = null;
        PostDrawStars = null;
    }

    #endregion
}
