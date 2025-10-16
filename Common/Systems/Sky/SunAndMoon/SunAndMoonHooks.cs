using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Terraria;
using ZensSky.Common.DataStructures;
using ZensSky.Core.ModCall;

namespace ZensSky.Common.Systems.Sky.SunAndMoon;

public static class SunAndMoonHooks
{
    #region Public Properties

    /// <summary>
    /// Additional moon styles based on <see cref="Main.moonType"/>.
    /// </summary>
    public static Dictionary<int, Asset<Texture2D>> ExtraMoonStyles { get; private set; } = [];

    #endregion

    #region Public Hooks

    #region Sun

    /// <returns><see cref="true"/> if the normal sun drawing should be used.</returns>
    public delegate bool hook_PreDrawSun(
        SpriteBatch spriteBatch,
        ref Vector2 position,
        ref Color color,
        ref float rotation,
        ref float scale,
        GraphicsDevice device);

    /// <inheritdoc cref="hook_PreDrawSun"/>
    [method: ModCall] // add_PreDrawSun, remove_PreDrawSun.
    public static event hook_PreDrawSun? PreDrawSun;

    /// <summary>
    /// Used for drawing after the (high-res) sun draws. Useful for things similar to the vanilla sunglasses effect.
    /// </summary>
    public delegate void hook_PostDrawSun(
        SpriteBatch spriteBatch,
        Vector2 position,
        Color color,
        float rotation,
        float scale,
        GraphicsDevice device);

    /// <inheritdoc cref="hook_PostDrawSun"/>
    [method: ModCall] // add_PostDrawSun, remove_PostDrawSun.
    public static event hook_PostDrawSun? PostDrawSun;

    #endregion

    #region Moon

    /// <summary>
    /// Used for modifying the moon texture without being tied to <see cref="Main.moonType"/>.
    /// </summary>
    public delegate void hook_ModifyMoonTexture(ref Asset<Texture2D> moon, bool eventMoon);

    /// <inheritdoc cref="hook_ModifyMoonTexture"/>
    [method: ModCall] // add_ModifyMoonTexture, remove_ModifyMoonTexture.
    public static event hook_ModifyMoonTexture? ModifyMoonTexture;

    /// <param name="moon">The high res moon texture to be used. If indended to be modified without custom drawing return <see cref="true"/></param>
    /// <param name="drawExtras">Weither or not vanilla/base moon details (e.g. moon2 rings,) should be drawn.</param>
    /// <param name="eventMoon">If a vanilla moon change (e.g. Frost Moon, Drunk World Moon) is active.</param>
    /// <returns><see cref="true"/> if the normal moon drawing should be used.</returns>
    public delegate bool hook_PreDrawMoon(
        SpriteBatch spriteBatch,
        ref Asset<Texture2D> moon,
        ref Vector2 position,
        ref Color color,
        ref float rotation,
        ref float scale,
        ref Color moonColor,
        ref Color shadowColor,
        ref bool drawExtras,
        bool eventMoon,
        GraphicsDevice device);

    /// <inheritdoc cref="hook_PreDrawMoon"/>
    [method: ModCall] // add_PreDrawMoon, remove_PreDrawMoon.
    public static event hook_PreDrawMoon? PreDrawMoon;

    /// <param name="moon">The high res moon texture to be used.</param>
    /// <param name="eventMoon">If NO vanilla moon change (e.g. Frost Moon, Drunk World Moon) is active.</param>
    public delegate void hook_PostDrawMoon(
        SpriteBatch spriteBatch,
        Asset<Texture2D> moon,
        Vector2 position,
        Color color,
        float rotation,
        float scale,
        Color moonColor,
        Color shadowColor,
        bool eventMoon,
        GraphicsDevice device);

    /// <inheritdoc cref="hook_PostDrawMoon"/>
    [method: ModCall] // add_PostDrawMoon, remove_PostDrawMoon.
    public static event hook_PostDrawMoon? PostDrawMoon;

    #region Extras

    /// <summary>
    /// Used for adding details to the base moon style, below the layer of PreDrawMoon.
    /// </summary>
    /// <param name="moon">The high res moon texture to be used. If indended to be modified without custom drawing return <see cref="true"/></param>
    /// <param name="eventMoon">If a vanilla moon change (e.g. Frost Moon, Drunk World Moon) is active.</param>
    /// <returns><see cref="true"/> if the normal moon drawing should be used.</returns>
    public delegate bool hook_PreDrawMoonExtras(
        SpriteBatch spriteBatch,
        ref Asset<Texture2D> moon,
        ref Vector2 position,
        ref Color color,
        ref float rotation,
        ref float scale,
        ref Color moonColor,
        ref Color shadowColor,
        bool eventMoon,
        GraphicsDevice device);

    /// <inheritdoc cref="hook_PreDrawMoonExtras"/>
    [method: ModCall] // add_PreDrawMoonExtras, remove_PreDrawMoonExtras.
    public static event hook_PreDrawMoonExtras? PreDrawMoonExtras;

    /// <summary>
    /// Used for adding details to the base moon style, below the layer of PostDrawMoon.
    /// </summary>
    /// <inheritdoc cref="hook_PostDrawMoon"/>
    [method: ModCall] // add_PostDrawMoonExtras, remove_PostDrawMoonExtras.
    public static event hook_PostDrawMoon? PostDrawMoonExtras;

    #endregion

    #endregion

    /// <returns><see cref="true"/> if the normal sun and moon drawing should be used.</returns>
    public delegate bool hook_PreDrawSunAndMoon(SpriteBatch spriteBatch);

    /// <inheritdoc cref="hook_PreDrawSunAndMoon"/>
    [method: ModCall] // add_PreDrawSunAndMoon, remove_PreDrawSunAndMoon.
    public static event hook_PreDrawSunAndMoon? PreDrawSunAndMoon;

    /// <returns><see cref="true"/> if the normal sun and moon drawing should be used.</returns>
    public delegate void hook_PostDrawSunAndMoon(SpriteBatch spriteBatch);

    /// <inheritdoc cref="hook_PostDrawSunAndMoon"/>
    [method: ModCall] // add_PostDrawSunAndMoon, remove_PostDrawSunAndMoon.
    public static event hook_PostDrawSunAndMoon? PostDrawSunAndMoon;

    public delegate void hook_OnUpdateSunAndMoonInfo(SunAndMoonInfo info);

    public static event hook_OnUpdateSunAndMoonInfo? OnUpdateSunAndMoonInfo;

    #endregion

    #region Public Methods

        // Methods below are mainly included for Mod.Call support.
    #region Sun

    [ModCall]
    public static void AddPreDrawSun(hook_PreDrawSun preDraw) =>
        PreDrawSun += preDraw;

    [ModCall]
    public static void AddPostDrawSun(hook_PostDrawSun postDraw) =>
        PostDrawSun += postDraw;

    [ModCall("PreDrawSun")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InvokePreDrawSun(
        SpriteBatch spriteBatch,
        ref Vector2 position,
        ref Color color,
        ref float rotation,
        ref float scale,
        GraphicsDevice device)
    {
        bool ret = true;

        if (PreDrawSun is null)
            return true;

        foreach (hook_PreDrawSun handler in
            PreDrawSun.GetInvocationList().Select(h => (hook_PreDrawSun)h))
            ret &= handler(spriteBatch, ref position, ref color, ref rotation, ref scale, device);

        return ret;
    }

    [ModCall("PostDrawSun")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InvokePostDrawSun(
        SpriteBatch spriteBatch,
        Vector2 position,
        Color color,
        float rotation,
        float scale,
        GraphicsDevice device) =>
        PostDrawSun?.Invoke(spriteBatch, position, color, rotation, scale, device);

    #endregion

    #region Moon

    [ModCall("CreateMoonStyle", "AddMoonTexture")]
    public static void AddMoonStyle(int index, Asset<Texture2D> texture) =>
        ExtraMoonStyles.Add(index, texture);

    [ModCall]
    public static void AddModifyMoonTexture(hook_ModifyMoonTexture modify) =>
        ModifyMoonTexture += modify;

    [ModCall]
    public static void AddPreDrawMoon(hook_PreDrawMoon preDraw) =>
        PreDrawMoon += preDraw;

    [ModCall]
    public static void AddPostDrawMoon(hook_PostDrawMoon postDraw) =>
        PostDrawMoon += postDraw;

    [ModCall("ModifyMoonTexture")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InvokeModifyMoonTexture(ref Asset<Texture2D> moon, bool eventMoon) =>
        ModifyMoonTexture?.Invoke(ref moon, eventMoon);

    [ModCall("PreDrawMoon")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InvokePreDrawMoon(
        SpriteBatch spriteBatch,
        ref Asset<Texture2D> moon,
        ref Vector2 position,
        ref Color color,
        ref float rotation,
        ref float scale,
        ref Color moonColor,
        ref Color shadowColor,
        ref bool drawExtras,
        bool eventMoon,
        GraphicsDevice device)
    {
        bool ret = true;

        if (PreDrawMoon is null)
            return true;

        foreach (hook_PreDrawMoon handler in
            PreDrawMoon.GetInvocationList().Select(h => (hook_PreDrawMoon)h))
            ret &= handler(spriteBatch, ref moon, ref position, ref color, ref rotation, ref scale, ref moonColor, ref shadowColor, ref drawExtras, eventMoon, device);

        return ret;
    }

    [ModCall("PostDrawMoon")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InvokePostDrawMoon(
        SpriteBatch spriteBatch,
        Asset<Texture2D> moon,
        Vector2 position,
        Color color,
        float rotation,
        float scale,
        Color moonColor,
        Color shadowColor,
        bool eventMoon,
        GraphicsDevice device) =>
        PostDrawMoon?.Invoke(spriteBatch, moon, position, color, rotation, scale, moonColor, shadowColor, eventMoon, device);

    #region Extras

    [ModCall]
    public static void AddPreDrawMoonExtras(hook_PreDrawMoonExtras preDrawExtras) =>
        PreDrawMoonExtras += preDrawExtras;

    [ModCall]
    public static void AddPostDrawMoonExtras(hook_PostDrawMoon postDrawExtras) =>
        PostDrawMoonExtras += postDrawExtras;

    [ModCall("PreDrawMoonExtras")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InvokePreDrawMoonExtras(
        SpriteBatch spriteBatch,
        ref Asset<Texture2D> moon,
        ref Vector2 position,
        ref Color color,
        ref float rotation,
        ref float scale,
        ref Color moonColor,
        ref Color shadowColor,
        bool eventMoon,
        GraphicsDevice device)
    {
        bool ret = true;

        if (PreDrawMoonExtras is null)
            return true;

        foreach (hook_PreDrawMoonExtras handler in
            PreDrawMoonExtras.GetInvocationList().Select(h => (hook_PreDrawMoonExtras)h))
            ret &= handler(spriteBatch, ref moon, ref position, ref color, ref rotation, ref scale, ref moonColor, ref shadowColor, eventMoon, device);

        return ret;
    }

    [ModCall("PostDrawMoonExtras")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InvokePostDrawMoonExtras(
        SpriteBatch spriteBatch,
        Asset<Texture2D> moon,
        Vector2 position,
        Color color,
        float rotation,
        float scale,
        Color moonColor,
        Color shadowColor,
        bool eventMoon,
        GraphicsDevice device) =>
        PostDrawMoonExtras?.Invoke(spriteBatch, moon, position, color, rotation, scale, moonColor, shadowColor, eventMoon, device);

    #endregion

    #endregion

    [ModCall]
    public static void AddPreDrawSunAndMoon(hook_PreDrawSunAndMoon preDraw) =>
        PreDrawSunAndMoon += preDraw;

    [ModCall]
    public static void AddPostDrawSunAndMoon(hook_PostDrawSunAndMoon postDraw) =>
        PostDrawSunAndMoon += postDraw;

    [ModCall("PreDrawSunAndMoon")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InvokePreDrawSunAndMoon(SpriteBatch spriteBatch)
    {
        bool ret = true;

        if (PreDrawSunAndMoon is null)
            return true;

        foreach (hook_PreDrawSunAndMoon handler in
            PreDrawSunAndMoon.GetInvocationList().Select(h => (hook_PreDrawSunAndMoon)h))
            ret &= handler(spriteBatch);

        return ret;
    }

    [ModCall("PostDrawSunAndMoon")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InvokePostDrawSunAndMoon(SpriteBatch spriteBatch) =>
        PostDrawSunAndMoon?.Invoke(spriteBatch);

    public static void InvokeOnUpdateSunAndMoonInfo(SunAndMoonInfo info) =>
        OnUpdateSunAndMoonInfo?.Invoke(info);

    public static void Clear()
    {
        PreDrawSun = null;
        PostDrawSun = null;

        ExtraMoonStyles.Clear();

        ModifyMoonTexture = null;

        PreDrawMoon = null;
        PostDrawMoon = null;

        PreDrawMoonExtras = null;
        PostDrawMoonExtras = null;

        OnUpdateSunAndMoonInfo = null;

        PreDrawSunAndMoon = null;
        PostDrawSunAndMoon = null;
    }

    #endregion
}
