using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.ModLoader.Config.UI;
using Terraria.UI;
using ZensSky.Core.Exceptions;
using ZensSky.Core.Utils;
using static System.Reflection.BindingFlags;

namespace ZensSky.Core.Config;

public sealed class UseSplitPanelSystem : ModSystem
{
    #region Private Fields

    private static ILHook? PatchDrawSelf;

    private static HashSet<Type> Types = [];

    #endregion

    #region Loading

    public override void Load()
    {
        Assembly assembly = Mod.Code;

        Types = [.. assembly.GetAllDecoratedTypes<UseSplitPanelAttribute>()];

        MethodInfo? drawSelf = typeof(ConfigElement).GetMethod("DrawSelf", NonPublic | Instance);

        if (drawSelf is not null)
            PatchDrawSelf = new(drawSelf,
                SkipRangeElementDrawing);
    }

    public override void Unload() =>
        PatchDrawSelf?.Dispose();

    private void SkipRangeElementDrawing(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            ILLabel jumpDrawPanel = c.DefineLabel();

            int elementIndex = -1;

            int spriteBatchIndex = -1;

            int colorIndex = -1;

            c.GotoNext(MoveType.After,
                i => i.MatchLdarg(out elementIndex),
                i => i.MatchLdarg(out _),
                i => i.MatchCall<UIElement>("DrawSelf"));

            c.GotoNext(MoveType.After,
                i => i.MatchLdloc(out colorIndex),
                i => i.MatchCall<ConfigElement>(nameof(ConfigElement.DrawPanel2)));

            c.MarkLabel(jumpDrawPanel);

            c.GotoPrev(MoveType.Before,
                i => i.MatchLdarg(out spriteBatchIndex),
                i => i.MatchLdloc(out _),
                i => i.MatchLdsfld(typeof(TextureAssets).FullName ?? "Terraria.GameContent.TextureAssets", nameof(TextureAssets.SettingsPanel)));

            c.MoveAfterLabels();

            c.EmitLdarg(elementIndex);

            c.EmitLdarg(spriteBatchIndex);
            c.EmitLdloc(colorIndex);

            c.EmitDelegate((ConfigElement element, SpriteBatch spriteBatch, Color color) =>
            {
                if (!Types.Contains(element.GetType()))
                    return false;

                Rectangle dims = element.Dimensions;

                Utilities.DrawSplitConfigPanel(spriteBatch, color, dims);

                return true;
            });

            c.EmitBrtrue(jumpDrawPanel);
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    #endregion
}
