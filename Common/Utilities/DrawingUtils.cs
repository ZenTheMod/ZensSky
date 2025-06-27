using Microsoft.Xna.Framework.Graphics;

namespace ZensSky.Common.Utilities;

public static class DrawingUtils
{
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
