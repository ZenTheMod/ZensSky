using Microsoft.Xna.Framework.Graphics;
using Terraria;
using ZensSky.Common.Utilities;

namespace ZensSky.Common.DataStructures;

public readonly ref struct RenderTargetSwap
{
    private RenderTargetBinding[] OldTargets { get; init; }

    #region Public Constructors

    public RenderTargetSwap(RenderTarget2D? target)
    {
        GraphicsDevice device = Main.instance.GraphicsDevice;

        OldTargets = device.GetRenderTargets();

        foreach (RenderTargetBinding oldTarget in OldTargets)
            if (oldTarget.RenderTarget is RenderTarget2D rt)
                rt.RenderTargetUsage = RenderTargetUsage.PreserveContents;

        device.SetRenderTarget(target);
    }

    public RenderTargetSwap(ref RenderTarget2D? target, int width, int height)
    {
        GraphicsDevice device = Main.instance.GraphicsDevice;

        OldTargets = device.GetRenderTargets();

        foreach (RenderTargetBinding oldTarget in OldTargets)
            if (oldTarget.RenderTarget is RenderTarget2D rt)
                rt.RenderTargetUsage = RenderTargetUsage.PreserveContents;

        DrawingUtils.ReintializeTarget(ref target, device, width, height);

        device.SetRenderTarget(target);
    }

    #endregion

    public void Dispose()
    {
        GraphicsDevice device = Main.instance.GraphicsDevice;

        device.SetRenderTargets(OldTargets);
    }
}
