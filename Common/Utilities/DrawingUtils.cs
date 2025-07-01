using Microsoft.Xna.Framework.Graphics;

namespace ZensSky.Common.Utilities;

public static class DrawingUtils
{
    #region RenderTargets
    
    /// <summary>
    /// Reinitializes <paramref name="target"/> if needed.
    /// </summary>
    /// <param name="target"></param>
    /// <param name="device"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    public static void ReintializeTarget(ref RenderTarget2D? target, 
        GraphicsDevice device,
        int width,
        int height,
        bool mipMap = false,
        SurfaceFormat preferredFormat = SurfaceFormat.Color,
        DepthFormat preferredDepthFormat = DepthFormat.None,
        int preferredMultiSampleCount = 0,
        RenderTargetUsage usage = RenderTargetUsage.PreserveContents)
    {
        if (target is null ||
            target.IsDisposed ||
            target.Width != width ||
            target.Height != height)
        {
            target?.Dispose();
            target = new(device,
                width,
                height,
                mipMap,
                preferredFormat,
                preferredDepthFormat,
                preferredMultiSampleCount,
                usage);
        }
    }

    #endregion
}
