using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Terraria;
using ZensSky.Core.Systems.ModCall;

namespace ZensSky.Common.Systems.SunAndMoon;

public static class SunAndMoonHooks
{
    #region Public Properties

    /// <summary>
    /// Additional moon styles based on <see cref="Main.moonType"/>.
    /// </summary>
    public static Dictionary<int, Asset<Texture2D>> ExtraMoonStyles { get; private set; } = [];

    #endregion

    #region Public Hooks

    /// <summary>
    /// Used for modifying the moon texture without being tied to <see cref="Main.moonType"/>.
    /// </summary>
    public delegate void hook_ModifyMoonTexture(ref Asset<Texture2D> moon, bool nonEventMoon);

    /// <inheritdoc cref="hook_ModifyMoonTexture"/>
    [method: ModCall]
    public static event hook_ModifyMoonTexture? ModifyMoonTexture;

    /// <summary>
    /// Used for moon styles that may require custom drawing to create an high-res counterpart.
    /// </summary>
    /// <param name="moon">The high res moon texture to be used. If indended to be modified without custom drawing return <see cref="true"/></param>
    /// <param name="nonEventMoon">If NO vanilla moon change (e.g. Frost Moon, Drunk World Moon) is active.</param>
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
        GraphicsDevice device,
        bool nonEventMoon);

    /// <inheritdoc cref="hook_PreDrawMoon"/>
    [method: ModCall]
    public static event hook_PreDrawMoon? PreDrawMoon;

    /// <summary>
    /// Used for moon styles that may require custom drawing to create an high-res counterpart.
    /// </summary>
    /// <param name="moon">The high res moon texture to be used.</param>
    /// <param name="nonEventMoon">If NO vanilla moon change (e.g. Frost Moon, Drunk World Moon) is active.</param>
    public delegate void hook_PostDrawMoon(
        SpriteBatch spriteBatch,
        Asset<Texture2D> moon,
        Vector2 position,
        Color color,
        float rotation,
        float scale,
        Color moonColor,
        Color shadowColor,
        GraphicsDevice device,
        bool nonEventMoon);

    /// <inheritdoc cref="hook_PostDrawMoon"/>
    [method: ModCall]
    public static event hook_PostDrawMoon? PostDrawMoon;

    #endregion

    #region Public Methods

        // Methods below are mainly included for Mod.Call support.
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
    public static void InvokeModifyMoonTexture(ref Asset<Texture2D> moon, bool nonEventMoon) =>
        ModifyMoonTexture?.Invoke(ref moon, nonEventMoon);

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
        GraphicsDevice device,
        bool nonEventMoon)
    {
        bool ret = true;

        if (PreDrawMoon is null)
            return true;

        foreach (hook_PreDrawMoon handler in
            PreDrawMoon.GetInvocationList().Select(h => (hook_PreDrawMoon)h))
            ret &= handler(spriteBatch, ref moon, ref position, ref color, ref rotation, ref scale, ref moonColor, ref shadowColor, device, nonEventMoon);

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
        GraphicsDevice device,
        bool nonEventMoon) =>
        PostDrawMoon?.Invoke(spriteBatch, moon, position, color, rotation, scale, moonColor, shadowColor, device, nonEventMoon);

    public static void Clear()
    {
        ExtraMoonStyles.Clear();

        ModifyMoonTexture = null;

        PreDrawMoon = null;
        PostDrawMoon = null;
    }

    #endregion
}
