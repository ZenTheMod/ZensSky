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

    public override void Load()
    {
        IL_Main.DrawSurfaceBG += ApplyCloudShader;
    }

    public override void Unload()
    {
        IL_Main.DrawSurfaceBG -= ApplyCloudShader;
    }

    #endregion

        // This is scuffed.
    private void ApplyCloudShader(ILContext il)
    {
        ILCursor c = new(il);

            // Add our snapshot as a local.
        VariableDefinition iHaveTrustIssues = new(il.Import(typeof(SpriteBatchSnapshot)));
        il.Body.Variables.Add(iHaveTrustIssues);

        #region I Hate Copy Pasting

        Func<Instruction, bool>[] middleLoop1Start = [
            i => i.MatchBr(out _),
            i => i.MatchLdsfld<Main>("spriteBatch"),
            i => i.MatchLdsfld("Terraria.GameContent.TextureAssets", "Background"),
            i => i.MatchLdsfld<Main>("cloudBG"),
            i => i.MatchLdcI4(0),
            i => i.MatchLdelemI4()];

        Func<Instruction, bool>[] middleLoop2Start = [
            i => i.MatchBr(out _),
            i => i.MatchLdsfld<Main>("spriteBatch"),
            i => i.MatchLdsfld("Terraria.GameContent.TextureAssets", "Background"),
            i => i.MatchLdsfld<Main>("cloudBG"),
            i => i.MatchLdcI4(1),
            i => i.MatchLdelemI4()];

        Func<Instruction, bool>[] middleLoop1End = [
            i => i.MatchLdloc(21),
            i => i.MatchLdcI4(1),
            i => i.MatchAdd(),
            i => i.MatchStloc(21),

            i => i.MatchLdloc(21),
            i => i.MatchLdarg0(),
            i => i.MatchLdfld<Main>("bgLoops"),
            i => i.MatchBlt(out _)];

        Func<Instruction, bool>[] middleLoop2End = [
            i => i.MatchLdloc(22),
            i => i.MatchLdcI4(1),
            i => i.MatchAdd(),
            i => i.MatchStloc(22),

            i => i.MatchLdloc(22),
            i => i.MatchLdarg0(),
            i => i.MatchLdfld<Main>("bgLoops"),
            i => i.MatchBlt(out _)];

        #endregion

        #region Loop1

        // Match to before the loop.
        if (!c.TryGotoNext(MoveType.Before,
            i => i.MatchBr(out _),
            i => i.MatchLdsfld<Main>("cloud"),
            i => i.MatchLdloc(13),
            i => i.MatchLdelemRef(),
            i => i.MatchLdfld<Cloud>("active")))
        {
            Mod.Logger.Error("Could not add fancy cloud lighting, failed to match to before the first loop.");

            throw new ILPatchFailureException(Mod, il, null);
        }

            // Apply our shader.
        c.EmitLdloca(iHaveTrustIssues);
        c.EmitDelegate(ApplyShader);

            // Match to after the loop.
        if (!c.TryGotoNext(MoveType.After,
            i => i.MatchLdloc(13),
            i => i.MatchLdcI4(1),
            i => i.MatchAdd(),
            i => i.MatchStloc(13),

            i => i.MatchLdloc(13),
            i => i.MatchLdcI4(200),
            i => i.MatchBlt(out _)))
        {
            Mod.Logger.Error("Could not add fancy cloud lighting, failed to match to after the first loop.");

            throw new ILPatchFailureException(Mod, il, null);
        }

        c.EmitLdloc(iHaveTrustIssues);
        c.EmitDelegate(ResetSpritebatch);

        #endregion

            // I would normally merge these (2 and 3) into one to minimize on sb restarts, but theres a SkyManager.Instance.DrawToDepth call and I don't trust modders.
        #region Loop2

        // Match to before the loop.
        if (!c.TryGotoNext(MoveType.Before, middleLoop1Start))
        {
            Mod.Logger.Error("Could not add fancy cloud lighting, failed to match to before the second loop.");

            throw new ILPatchFailureException(Mod, il, null);
        }

            // Apply our shader.
        c.EmitLdloca(iHaveTrustIssues);
        c.EmitDelegate(ApplyShader);

            // Match to after the loop.
        if (!c.TryGotoNext(MoveType.After, middleLoop1End))
        {
            Mod.Logger.Error("Could not add fancy cloud lighting, failed to match to after the second loop.");

            throw new ILPatchFailureException(Mod, il, null);
        }

        c.EmitLdloc(iHaveTrustIssues);
        c.EmitDelegate(ResetSpritebatch);

        #endregion

        #region Loop3

            // Match to before the loop.
        if (!c.TryGotoNext(MoveType.Before, middleLoop2Start))
        {
            Mod.Logger.Error("Could not add fancy cloud lighting, failed to match to before the third loop.");

            throw new ILPatchFailureException(Mod, il, null);
        }

            // Apply our shader.
        c.EmitLdloca(iHaveTrustIssues);
        c.EmitDelegate(ApplyShader);

            // Match to after the loop.
        if (!c.TryGotoNext(MoveType.After, middleLoop2End))
        {
            Mod.Logger.Error("Could not add fancy cloud lighting, failed to match to after the third loop.");

            throw new ILPatchFailureException(Mod, il, null);
        }

        c.EmitLdloc(iHaveTrustIssues);
        c.EmitDelegate(ResetSpritebatch);

        #endregion

        #region Loop4

            // Match to before the loop.
        if (!c.TryGotoNext(MoveType.Before,
            i => i.MatchBr(out _),
            i => i.MatchLdsfld<Main>("cloud"),
            i => i.MatchLdloc(23),
            i => i.MatchLdelemRef(),
            i => i.MatchLdfld<Cloud>("active")))
        {
            Mod.Logger.Error("Could not add fancy cloud lighting, failed to match to before the forth loop.");

            throw new ILPatchFailureException(Mod, il, null);
        }

            // Apply our shader.
        c.EmitLdloca(iHaveTrustIssues);
        c.EmitDelegate(ApplyShader);

            // Match to after the loop.
        if (!c.TryGotoNext(MoveType.After,
            i => i.MatchLdloc(23),
            i => i.MatchLdcI4(1),
            i => i.MatchAdd(),
            i => i.MatchStloc(23),

            i => i.MatchLdloc(23),
            i => i.MatchLdcI4(200),
            i => i.MatchBlt(out _)))
        {
            Mod.Logger.Error("Could not add fancy cloud lighting, failed to match to after the forth loop.");

            throw new ILPatchFailureException(Mod, il, null);
        }

        c.EmitLdloc(iHaveTrustIssues);
        c.EmitDelegate(ResetSpritebatch);

        #endregion

        #region Loop5

            // Match to before the loop.
        if (!c.TryGotoNext(MoveType.Before,
            i => i.MatchBr(out _),
            i => i.MatchLdsfld<Main>("cloud"),
            i => i.MatchLdloc(31),
            i => i.MatchLdelemRef(),
            i => i.MatchLdfld<Cloud>("active")))
        {
            Mod.Logger.Error("Could not add fancy cloud lighting, failed to match to before the forth loop.");

            throw new ILPatchFailureException(Mod, il, null);
        }

            // Apply our shader.
        c.EmitLdloca(iHaveTrustIssues);
        c.EmitDelegate(ApplyShader);

            // Match to after the loop.
        if (!c.TryGotoNext(MoveType.After,
            i => i.MatchLdloc(31),
            i => i.MatchLdcI4(1),
            i => i.MatchAdd(),
            i => i.MatchStloc(31),

            i => i.MatchLdloc(31),
            i => i.MatchLdcI4(200),
            i => i.MatchBlt(out _)))
        {
            Mod.Logger.Error("Could not add fancy cloud lighting, failed to match to after the forth loop.");

            throw new ILPatchFailureException(Mod, il, null);
        }

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

        lighting.Parameters["SunPosition"]?.SetValue(SunAndMoonSystem.SunMoonPosition);

        Color color = GetColor();
        lighting.Parameters["SunColor"]?.SetValue(color.ToVector4());

            // TrollMadelineCelesteFace.png
        lighting.Parameters["UseEdgeDetection"]?.SetValue(SkyConfig.Instance.CloudsEdgeDetection);

        lighting.CurrentTechnique.Passes[0].Apply();

        Main.spriteBatch.End(out snapshot);
        Main.spriteBatch.Begin(snapshot.SortMode, snapshot.BlendState, snapshot.SamplerState, snapshot.DepthStencilState, snapshot.RasterizerState, lighting, snapshot.TransformationMatrix);
    }

    private void ResetSpritebatch(SpriteBatchSnapshot snapshot)
    {
        if (!SkyConfig.Instance.CloudsEnabled)
            return;

        Main.spriteBatch.End();
        Main.spriteBatch.Begin(in snapshot);
    }

    private Color GetColor()
    {
        Vector2 position = SunAndMoonSystem.SunMoonPosition;
        float centerX = MiscUtils.HalfScreenSize.X;

        float distanceFromCenter = MathF.Abs(centerX - position.X) / centerX;

        Color color = SunAndMoonSystem.SunMoonColor * 2f;
        color = color.MultiplyRGB(Main.dayTime ? SunMultiplier : MoonMultiplier);

            // Add a fadeinout effect so the color doesnt just suddenly pop up.
        color *= Utils.Remap(distanceFromCenter, FlareEdgeFallOffStart, FlareEdgeFallOffEnd, 1f, 0f);

        return color;
    }
}
