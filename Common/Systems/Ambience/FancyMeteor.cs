using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.Skies;
using Terraria.Utilities;
using ZensSky.Common.Systems.Stars;

namespace ZensSky.Common.Systems.Ambience;

public sealed class FancyMeteor(Player player, FastRandom random) : AmbientSky.MeteorSkyEntity(player, random)
{
    #region Private Fields

    private static readonly Vector4 StartColor = new(.28f, .2f, 1f, 1f);
    private static readonly Vector4 EndColor = new(.9f, .2f, .1f, 1f);

    private static readonly Vector2 Origin = new(.085f, .5f);

    private static readonly Vector2 Scale = new(2.5f, .18f);

    #endregion

    public override void Draw(SpriteBatch spriteBatch, float depthScale, float minDepth, float maxDepth)
    {
        Depth = 5.5f;

        if (Depth <= minDepth ||
            Depth > maxDepth ||
            !SkyEffects.Meteor.IsReady)
            return;

        spriteBatch.End(out var snapshot);
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, snapshot.DepthStencilState, snapshot.RasterizerState, null, snapshot.TransformMatrix);

        float alpha = Utils.Remap(StarSystem.StarAlpha, 0f, 1f, 0.3f, 0.55f);

        SkyEffects.Meteor.StartColor = StartColor * alpha;
        SkyEffects.Meteor.EndColor = EndColor * alpha;

        SkyEffects.Meteor.Time = Main.GlobalTimeWrappedHourly * .3f;

        SkyEffects.Meteor.Scale = 5f;

        SkyEffects.Meteor.Apply();

        Texture2D noise = MiscTextures.LoopingNoise;

        Vector2 position = GetDrawPositionByDepth() - Main.Camera.UnscaledPosition;

        Vector2 origin = Origin * noise.Size();

        Vector2 scale = Scale * (depthScale / Depth);

        spriteBatch.Draw(noise, position, null, Color.White, Rotation + MathHelper.PiOver2, origin, scale, Effects, 0f);

        spriteBatch.Restart(in snapshot);
    }
}
