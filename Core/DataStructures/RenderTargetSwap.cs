using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics.CodeAnalysis;
using Terraria;
using ZensSky.Core.Utils;

namespace ZensSky.Core.DataStructures;

#pragma warning disable CS8777

/// <summary>
/// Wraps <see cref="RenderTarget2D"/> application with a disposable pattern for use with a <see cref="using"/> statement.<br/><br/>
/// 
/// <example>
/// Example:<br/>
/// <code>
///     spriteBatch.End(out var snapshot);
///
///     using (new RenderTargetSwap(ref MyTarget, width, height))
///     {
///         device.Clear(/* color */);
///         
///         spriteBatch.Begin(/* whatever paramaters */);
///         
///             // Drawcode here.
///     
///         spriteBatch.End();
///     }
/// 
///     spriteBatch.Begin(in snapshot);
/// </code>
/// </example>
/// 
/// </summary>
public readonly ref struct RenderTargetSwap
{
    #region Private Properties

    private RenderTargetBinding[] OldTargets { get; init; }

    private Rectangle OldScissor { get; init; }

    #endregion

    #region Public Constructors

    public RenderTargetSwap(RenderTarget2D? target)
    {
        GraphicsDevice device = Main.instance.GraphicsDevice;

        OldTargets = device.GetRenderTargets();
        OldScissor = device.ScissorRectangle;

            // Set the default RenderTargetUsage to PreserveContents to prevent clearing the prior targets when swapping back in Dispose().
        foreach (RenderTargetBinding oldTarget in OldTargets)
            if (oldTarget.RenderTarget is RenderTarget2D rt)
                rt.RenderTargetUsage = RenderTargetUsage.PreserveContents;

        device.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;

        device.SetRenderTarget(target);
        device.ScissorRectangle = new(0, 0,
            target?.Width ?? Main.graphics.PreferredBackBufferWidth,
            target?.Height ?? Main.graphics.PreferredBackBufferHeight);
    }

    public RenderTargetSwap(
        [NotNull] ref RenderTarget2D? target,
        int width,
        int height,
        bool mipMap = false,
        SurfaceFormat preferredFormat = SurfaceFormat.Color,
        DepthFormat preferredDepthFormat = DepthFormat.None,
        int preferredMultiSampleCount = 0,
        RenderTargetUsage usage = RenderTargetUsage.PreserveContents)
    {
        GraphicsDevice device = Main.instance.GraphicsDevice;

        OldTargets = device.GetRenderTargets();
        OldScissor = device.ScissorRectangle;

        Utilities.ReintializeTarget(
            ref target,
            device,
            width,
            height,
            mipMap,
            preferredFormat,
            preferredDepthFormat,
            preferredMultiSampleCount,
            usage);

        foreach (RenderTargetBinding oldTarget in OldTargets)
            if (oldTarget.RenderTarget is RenderTarget2D rt)
                rt.RenderTargetUsage = RenderTargetUsage.PreserveContents;

        device.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;

        device.SetRenderTarget(target);
        device.ScissorRectangle = new(0, 0,
            target?.Width ?? Main.graphics.PreferredBackBufferWidth,
            target?.Height ?? Main.graphics.PreferredBackBufferHeight);
    }

    #endregion

    #region Disposable Pattern

    public void Dispose()
    {
        GraphicsDevice device = Main.instance.GraphicsDevice;

        device.SetRenderTargets(OldTargets);
        device.ScissorRectangle = OldScissor;
    }

    #endregion
}
