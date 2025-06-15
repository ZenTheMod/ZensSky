using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;

namespace ZensSky.Common.Utilities;

public static class DrawingUtils
{
    #region Pixelated SpriteBatch

    public static Matrix HalfScale => Matrix.CreateScale(0.5f);

        // This is pretty bad shorthand.
            // TODO: Fix this.
    public static void BeginToggledHalfScale(this SpriteBatch spriteBatch, SpriteSortMode sortMode, BlendState blendState, bool usingHalf)
    {
        if (usingHalf)
            spriteBatch.BeginHalfScale(sortMode, blendState);
        else
            spriteBatch.Begin(sortMode, blendState);
    }

    public static void BeginToggledHalfScale(this SpriteBatch spriteBatch, SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState, bool usingHalf)
    {
        if (usingHalf)
            spriteBatch.BeginHalfScale(sortMode, blendState, samplerState);
        else
            spriteBatch.Begin(sortMode, blendState, samplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise);
    }

    public static void BeginHalfScale(this SpriteBatch spriteBatch, SpriteSortMode sortMode, BlendState blendState) =>
        spriteBatch.Begin(sortMode, blendState, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, HalfScale);

    public static void BeginHalfScale(this SpriteBatch spriteBatch, SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState) =>
        spriteBatch.Begin(sortMode, blendState, samplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, HalfScale);

    #endregion

    #region RenderTargetContent

    /// <summary>
    /// Requests the <paramref name="renderTarget"/> and draws it if its ready.
    /// </summary>
    /// <param name="spriteBatch"></param>
    /// <param name="renderTarget"></param>
    public static void RequestAndDrawRenderTarget(this SpriteBatch spriteBatch, ARenderTargetContentByRequest? renderTarget)
    {
        renderTarget?.Request();
        if (renderTarget?.IsReady is true)
            spriteBatch.Draw(renderTarget.GetTarget(), Vector2.Zero, Color.White);
    }

    /// <summary>
    /// Requests the <paramref name="renderTarget"/> and draws it if its ready.
    /// </summary>
    /// <param name="spriteBatch"></param>
    /// <param name="renderTarget"></param>
    public static void RequestAndDrawRenderTarget(this SpriteBatch spriteBatch, ARenderTargetContentByRequest? renderTarget, Rectangle destination)
    {
        renderTarget?.Request();
        if (renderTarget?.IsReady is true)
            spriteBatch.Draw(renderTarget.GetTarget(), destination, Color.White);
    }

    #endregion

    #region RenderTargets

        // TODO: Boot to main thread.
    /// <summary>
    /// Reinitializes <paramref name="target"/> if needed.
    /// </summary>
    /// <param name="target"></param>
    /// <param name="device"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    public static void ReintializeTarget(ref RenderTarget2D? target, GraphicsDevice device, int width, int height)
    {
        if (target is null ||
            target.IsDisposed ||
            target.Width != width ||
            target.Height != height)
        {
            target?.Dispose();
            target = new(device, width, height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        }
    }

    #endregion
}
