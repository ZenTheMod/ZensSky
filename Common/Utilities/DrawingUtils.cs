using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using ZensSky.Common.DataStructures;

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

    #region SpriteBatchSnapshot

    /// <summary>
    /// Calls <see cref="SpriteBatch.Begin(SpriteSortMode, BlendState, SamplerState, DepthStencilState, RasterizerState, Effect?, Matrix)"/> with the data on <paramref name="snapshot"/>.
    /// </summary>
    /// <param name="spriteBatch"></param>
    /// <param name="snapshot"></param>
    public static void Begin(this SpriteBatch spriteBatch, in SpriteBatchSnapshot snapshot) =>
        spriteBatch.Begin(snapshot.SortMode, snapshot.BlendState, snapshot.SamplerState, snapshot.DepthStencilState, snapshot.RasterizerState, snapshot.Effect, snapshot.TransformationMatrix);

    /// <summary>
    /// Calls <see cref="SpriteBatch.End()"/> and outs <paramref name="spriteBatch"/>'s data as <paramref name="snapshot"/>.
    /// </summary>
    /// <param name="spriteBatch"></param>
    /// <param name="snapshot"></param>
    public static void End(this SpriteBatch spriteBatch, out SpriteBatchSnapshot snapshot)
    {
        snapshot = SpriteBatchSnapshot.Capture(spriteBatch);
        spriteBatch.End();
    }

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
}
