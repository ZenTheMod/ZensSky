using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using ZensSky.Common.Config;
using ZensSky.Common.Systems.Compat;
using static ZensSky.Common.Systems.Sky.SunAndMoon.SunAndMoonSystem;

namespace ZensSky.Common.Systems.Sky.Lighting;

public sealed class MoonLight : ISkyLight
{
    #region Private Fields

    private const float MoonSize = 1.2f;

    #endregion

    #region Public Properties

    public bool Active =>
        ShowMoon &&
        (RedSunSystem.IsEnabled ||
        !Main.dayTime);

    public Color Color =>
        GetLightColor(false);

    public Vector2 Position
    {
        get
        {
            Vector2 viewportSize =
                Main.instance.GraphicsDevice.Viewport.Bounds.Size();

            Vector2 moonPosition =
                Info.MoonPosition;

            if (Main.BackgroundViewMatrix.Effects.HasFlag(SpriteEffects.FlipVertically))
                moonPosition.Y = viewportSize.Y - moonPosition.Y;

            return moonPosition;
        }
    }

    public float Size =>
        Info.MoonScale * MoonSize;

    public Asset<Texture2D>? Texture =>
        SkyConfig.Instance.UseSunAndMoon ? MoonTexture : null;

    #endregion
}
