using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using ZensSky.Common.Registries;

namespace ZensSky.Common.Systems.ModIcon;

public sealed class SkyPanelTargetContent : ARenderTargetContentByRequest
{
    private static readonly Color Clear = new(70);

    public Vector2 Size { get; set; }

    protected override void HandleUseReqest(GraphicsDevice device, SpriteBatch spriteBatch)
    {
        ArgumentNullException.ThrowIfNull(device, nameof(device));
        ArgumentNullException.ThrowIfNull(spriteBatch, nameof(spriteBatch));

        PrepareARenderTarget_AndListenToEvents(ref _target, device, (int)Size.X, (int)Size.Y, (RenderTargetUsage)1);

        device.SetRenderTarget(_target);
        device.Clear(Clear);

        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);

        DrawPlanet(device, spriteBatch);

        spriteBatch.End();

        device.SetRenderTarget(null);
        _wasPrepared = true;
    }

    private void DrawPlanet(GraphicsDevice device, SpriteBatch spriteBatch)
    {
        Effect planet = Shaders.Planet.Value;

        if (planet is null)
            return;

        planet.Parameters["shadowColor"]?.SetValue(Color.Black.ToVector4());

        planet.Parameters["planetRotation"]?.SetValue(0f);
        planet.Parameters["shadowRotation"]?.SetValue(Main.GlobalTimeWrappedHourly);

        planet.Parameters["falloffStart"]?.SetValue(1f);
        planet.CurrentTechnique.Passes[0].Apply();

        spriteBatch.Draw(Textures.Pixel.Value, new(20, Size.Y - 20), null, Color.White, 0f, Vector2.Zero, 900f, SpriteEffects.None, 0f);
    }
}
