using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using ZensSky.Core.Utils;
using ZensSky.Core.Exceptions;
using static System.Reflection.BindingFlags;
using static ZensSky.Common.Systems.Menu.Controllers.ButtonColorController;
using static ZensSky.Common.Systems.Menu.MenuControllerSystem;

namespace ZensSky.Common.Systems.Compat;

/// <summary>
/// Edits and Hooks:
/// <list type="bullet">
///     <item>
///         <see cref="AddToggle"/><br/>
///         Adds a small toggle in the style of a dropdown (▼ : ▲) to the main menu, on the right side of the button to change menu styles.
///     </item>
///     <item>
///         <see cref="ModifyCoreToggle"/><br/>
///         Disables interactions with the core menu toggle while hovering the controller interface.
///     </item>
///     <item>
///         <see cref="ModifyButtons"/><br/>
///         Disables interactions with main menu buttons while hovering the controller interface.<br/>
///         Additonally modifies the color of the buttons.
///     </item>
/// </list>
/// </summary>
[JITWhenModsEnabled("CoolerMenu")]
[ExtendsFromMod("CoolerMenu")]
[Autoload(Side = ModSide.Client)]
public sealed class CoolerMenuSystem : ModSystem
{
    #region Private Fields

    private static ILHook? PatchRenderVanillaMenuToggle;

    private static ILHook? PatchRenderCoreMenuToggle;

    private static ILHook? PatchDrawFloatingButton;

    #endregion

    #region Public Fields

    public const int CoolerMenuID = 1007;

    #endregion

    #region Public Properties

    public static bool IsEnabled { get; private set; }

    #endregion

    #region Loading

        // MainThreadSystem.Enqueue can be ignored as this mod is loaded first regardless.
    public override void Load()
    {
        IsEnabled = true;

        MethodInfo? renderVanillaMenuToggle = typeof(CoolerMenu.CoolerMenu).GetMethod(nameof(CoolerMenu.CoolerMenu.RenderVanillaMenuToggle), NonPublic | Instance);

        if (renderVanillaMenuToggle is not null)
            PatchRenderVanillaMenuToggle = new(renderVanillaMenuToggle,
                AddToggle);

        MethodInfo? renderCoreMenuToggle = typeof(CoolerMenu.CoolerMenu).GetMethod(nameof(CoolerMenu.CoolerMenu.RenderCoreMenuToggle), NonPublic | Instance);

        if (renderCoreMenuToggle is not null)
            PatchRenderCoreMenuToggle = new(renderCoreMenuToggle,
                ModifyCoreToggle);

        MethodInfo? drawFloatingButton = typeof(CoolerMenu.CoolerMenu).GetMethod(nameof(CoolerMenu.CoolerMenu.DrawFloatingButton), NonPublic | Instance);

        if (drawFloatingButton is not null)
            PatchDrawFloatingButton = new(drawFloatingButton,
                ModifyButtons);
    }

    public override void Unload()
    {
        PatchRenderVanillaMenuToggle?.Dispose();
        PatchRenderCoreMenuToggle?.Dispose();

        PatchDrawFloatingButton?.Dispose();
    }

    #endregion

    #region Toggles

    private void AddToggle(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            int switchTextRectIndex = -1;

            ILLabel? jumpInteractionsTarget = c.DefineLabel();

                // Match to before the menu switch text is drawn.
            c.GotoNext(MoveType.Before,
                i => i.MatchLdloca(out switchTextRectIndex),
                i => i.MatchLdsfld<Main>(nameof(Main.mouseX)),
                i => i.MatchLdsfld<Main>(nameof(Main.mouseY)),
                i => i.MatchCall<Rectangle>(nameof(Rectangle.Contains)),
                i => i.MatchBrfalse(out jumpInteractionsTarget));

            c.EmitDelegate(() => Hovering);

            c.EmitBrtrue(jumpInteractionsTarget);

                // Match to before the menu switch text is drawn.
            c.GotoNext(MoveType.Before,
                i => i.MatchLdsfld<Main>(nameof(Main.spriteBatch)),
                i => i.MatchLdsfld(typeof(FontAssets).FullName ?? "Terraria.GameContent.FontAssets", nameof(FontAssets.MouseText)));

            c.MoveAfterLabels();

            c.EmitLdloc(switchTextRectIndex);

                // Add our dropdown menu button.
            c.EmitCall(DrawToggle);
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    private void ModifyCoreToggle(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            ILLabel? jumpInteractionsTarget = c.DefineLabel();

                // Match to before the menu switch text is drawn.
            c.GotoNext(MoveType.Before,
                i => i.MatchLdloca(out _),
                i => i.MatchLdsfld<Main>(nameof(Main.mouseX)),
                i => i.MatchLdsfld<Main>(nameof(Main.mouseY)),
                i => i.MatchCall<Rectangle>(nameof(Rectangle.Contains)),
                i => i.MatchBrfalse(out jumpInteractionsTarget));

            c.EmitDelegate(() => Hovering);

            c.EmitBrtrue(jumpInteractionsTarget);
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    #endregion

    #region Buttons

    private void ModifyButtons(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            int hoveringIndex = -1;

            int colorIndex = -1;

            c.GotoNext(MoveType.After,
                i => i.MatchCall(typeof(Utils).FullName ?? "Terraria.Utils", nameof(Utils.ToPoint)),
                i => i.MatchCall<Rectangle>(nameof(Rectangle.Contains)),
                i => i.MatchStloc(out hoveringIndex));

            c.EmitLdloca(hoveringIndex);

            c.EmitDelegate((ref bool hovering) =>
                { hovering &= !Hovering; });

            c.GotoNext(MoveType.After,
                i => i.MatchLdcI4(out _),
                i => i.MatchLdcI4(out _),
                i => i.MatchLdcI4(out _),
                i => i.MatchNewobj<Color>(),
                i => i.MatchStloc(out colorIndex));

            c.MoveAfterLabels();

            c.EmitLdloc(hoveringIndex);

            c.EmitLdloca(colorIndex);

            c.EmitDelegate((bool hovering, ref Color color) =>
                { ModifyColor(ref color, Color.White, hovering.ToInt()); });
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    #endregion
}
