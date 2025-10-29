using Microsoft.Xna.Framework.Graphics;
using Terraria;
using static ZensSky.Common.Systems.Sky.SunAndMoon.SunAndMoonSystem;

namespace ZensSky.Common.Systems.Sky.Lighting;

public sealed class SunLight : SkyLight
{
    #region Private Fields

    private const float SunSize = 4.4f;

    #endregion

    #region Public Properties

    public override bool Active =>
        ShowSun &&
        Main.dayTime;

    protected override Color Color =>
        GetLightColor(true);

    protected override Vector2 Position
    {
        get
        {
            Vector2 viewportSize =
                Main.instance.GraphicsDevice.Viewport.Bounds.Size();

            Vector2 sunPosition =
                Info.SunPosition;

            if (Main.BackgroundViewMatrix.Effects.HasFlag(SpriteEffects.FlipVertically))
                sunPosition.Y = viewportSize.Y - sunPosition.Y;

            return sunPosition;
        }
    }

    protected override float Size =>
        Info.SunScale * SunSize;

    #endregion
}
