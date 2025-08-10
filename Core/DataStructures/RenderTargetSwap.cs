using Microsoft.Xna.Framework.Graphics;
using Terraria;
using ZensSky.Core.Utils;

namespace ZensSky.Core.DataStructures;

/// <summary>
/// Allows for esay swapping and use of a <see cref="RenderTarget2D"/>, with the <see cref="using"/> keyword.
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

            // Set the default RenderTargetUsage to PreserveContents to prevent causing black screens when swaping targets.
        foreach (RenderTargetBinding oldTarget in OldTargets)
            if (oldTarget.RenderTarget is RenderTarget2D rt)
                rt.RenderTargetUsage = RenderTargetUsage.PreserveContents;

        device.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;

        device.SetRenderTarget(target);
        device.ScissorRectangle = new(0, 0,
            target?.Width ?? Main.graphics.PreferredBackBufferWidth,
            target?.Height ?? Main.graphics.PreferredBackBufferHeight);
    }

    public RenderTargetSwap(ref RenderTarget2D? target,
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

            // Set the default RenderTargetUsage to PreserveContents to prevent causing black screens when swaping targets.
        foreach (RenderTargetBinding oldTarget in OldTargets)
            if (oldTarget.RenderTarget is RenderTarget2D rt)
                rt.RenderTargetUsage = RenderTargetUsage.PreserveContents;

        device.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;

        Utilities.ReintializeTarget(ref target,
            device,
            width,
            height,
            mipMap,
            preferredFormat,
            preferredDepthFormat,
            preferredMultiSampleCount,
            usage);

        device.SetRenderTarget(target);
        device.ScissorRectangle = new(0, 0,
            target?.Width ?? Main.graphics.PreferredBackBufferWidth,
            target?.Height ?? Main.graphics.PreferredBackBufferHeight);
    }

    #endregion

    public void Dispose()
    {
        GraphicsDevice device = Main.instance.GraphicsDevice;

        device.SetRenderTargets(OldTargets);
        device.ScissorRectangle = OldScissor;
    }
}
