using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.DataStructures;
using ZensSky.Common.Registries;
using ZensSky.Common.Systems.SunAndMoon;
using ZensSky.Common.Utilities;

namespace ZensSky.Common.Systems.Clouds;

public sealed class CloudSystem : ModSystem
{
    #region Private Fields

    private const float FlareEdgeFallOffStart = 1f;
    private const float FlareEdgeFallOffEnd = 1.11f;

    private static readonly Color SunMultiplier = new(255, 245, 225);
    private static readonly Color MoonMultiplier = new(33, 27, 47);

    #endregion

    #region Loading

    public override void Load() => IL_Main.DrawSurfaceBG += ApplyCloudShader;

    public override void Unload() => IL_Main.DrawSurfaceBG -= ApplyCloudShader;

    #endregion

        // This is scuffed.
    private void ApplyCloudShader(ILContext il)
    {
        ILCursor c = new(il);

            // Add our snapshot as a local.
        VariableDefinition iHaveTrustIssues = new(il.Import(typeof(SpriteBatchSnapshot)));
        il.Body.Variables.Add(iHaveTrustIssues);

        #region I Hate Copy Pasting

        Func<Instruction, bool>[] cloudLoopStart = [
            i => i.MatchBr(out _),
            i => i.MatchLdsfld<Main>("cloud"),
            i => i.MatchLdloc(out _),
            i => i.MatchLdelemRef(),
            i => i.MatchLdfld<Cloud>("active")];

        Func<Instruction, bool>[] cloudLoopEnd = [
            i => i.MatchLdloc(out _),
            i => i.MatchLdcI4(1),
            i => i.MatchAdd(),
            i => i.MatchStloc(out _),
            i => i.MatchLdloc(out _),
            i => i.MatchLdcI4(200),
            i => i.MatchBlt(out _)];

        Func<Instruction, bool>[] cloudBGLoopStart = [
            i => i.MatchBr(out _),
            i => i.MatchLdsfld<Main>("spriteBatch"),
            i => i.MatchLdsfld("Terraria.GameContent.TextureAssets", "Background"),
            i => i.MatchLdsfld<Main>("cloudBG"),
            i => i.MatchLdcI4(0),
            i => i.MatchLdelemI4()];

        Func<Instruction, bool>[] cloudBGLoopEnd = [
            i => i.MatchLdloc(22),
            i => i.MatchLdcI4(1),
            i => i.MatchAdd(),
            i => i.MatchStloc(22),
            i => i.MatchLdloc(22),
            i => i.MatchLdarg0(),
            i => i.MatchLdfld<Main>("bgLoops"),
            i => i.MatchBlt(out _)];

        #endregion

        #region Various Clouds

        for (int i = 0; i < 3; i++)
        {
                // Match to before the loop.
            if (!c.TryGotoNext(MoveType.Before, cloudLoopStart))
                throw new ILPatchFailureException(Mod, il, null);

                // Apply our shader.
            c.EmitLdloca(iHaveTrustIssues);
            c.EmitDelegate(ApplyShader);

                // Match to after the loop.
            if (!c.TryGotoNext(MoveType.After, cloudLoopEnd))
                throw new ILPatchFailureException(Mod, il, null);

            c.EmitLdloc(iHaveTrustIssues);
            c.EmitDelegate(ResetSpritebatch);
        }

        #endregion

        #region CloudBG

            // Match to before the loop.
        if (!c.TryGotoPrev(MoveType.Before, cloudBGLoopStart))
            throw new ILPatchFailureException(Mod, il, null);

            // Apply our shader.
        c.EmitLdloca(iHaveTrustIssues);
        c.EmitDelegate(ApplyShader);

            // Match to after the loop.
        if (!c.TryGotoNext(MoveType.After, cloudBGLoopEnd))
            throw new ILPatchFailureException(Mod, il, null);

        c.EmitLdloc(iHaveTrustIssues);
        c.EmitDelegate(ResetSpritebatch);

        #endregion
    }

    private void ApplyShader(ref SpriteBatchSnapshot snapshot)
    {
        Effect lighting = Shaders.Cloud.Value;

        if (!SkyConfig.Instance.CloudsEnabled || lighting is null)
            return;

        Viewport viewport = Main.instance.GraphicsDevice.Viewport;

        Vector2 viewportSize = viewport.Bounds.Size();
        lighting.Parameters["ScreenSize"]?.SetValue(viewportSize);

        Vector2 sunPosition = SunAndMoonSystem.SunMoonPosition;

        if (Main.BackgroundViewMatrix.Effects.HasFlag(SpriteEffects.FlipVertically))
            sunPosition.Y = viewportSize.Y - sunPosition.Y;
        lighting.Parameters["SunPosition"]?.SetValue(sunPosition);

        Color color = GetColor();
        lighting.Parameters["SunColor"]?.SetValue(color.ToVector4());

        lighting.CurrentTechnique.Passes[0].Apply();

        Main.spriteBatch.End(out snapshot);
        Main.spriteBatch.Begin(snapshot.SortMode, snapshot.BlendState, SamplerState.PointClamp, snapshot.DepthStencilState, snapshot.RasterizerState, lighting, snapshot.TransformationMatrix);
    }

    private void ResetSpritebatch(SpriteBatchSnapshot snapshot)
    {
        if (!SkyConfig.Instance.CloudsEnabled)
            return;

        Main.spriteBatch.End();
        Main.spriteBatch.Begin(in snapshot);
    }

    private static Color GetColor()
    {
        Vector2 position = SunAndMoonSystem.SunMoonPosition;
        float centerX = MiscUtils.HalfScreenSize.X;

        float distanceFromCenter = MathF.Abs(centerX - position.X) / centerX;

        Color color = SunAndMoonSystem.SunMoonColor * 2f;
        color = color.MultiplyRGB(Main.dayTime ? SunMultiplier : MoonMultiplier);

            // Add a fadeinout effect so the color doesnt just suddenly pop up.
        color *= Utils.Remap(distanceFromCenter, FlareEdgeFallOffStart, FlareEdgeFallOffEnd, 1f, 0f);
            // And lessen it at the lower part of the screen.
        color *= 1 - (position.Y / MiscUtils.ScreenSize.Y);

        return color;
    }
}
