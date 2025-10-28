using CoolerMenu.Common.Menu;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using ZensSky.Core.Exceptions;
using ZensSky.Core.Utils;
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

    private static ILHook? PatchDrawButton;

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

        MethodInfo? renderVanillaMenuToggle = typeof(MenuSystem).GetMethod(nameof(MenuSystem.RenderVanillaMenuToggle), NonPublic | Static);

        if (renderVanillaMenuToggle is not null)
            PatchRenderVanillaMenuToggle = new(renderVanillaMenuToggle,
                AddToggle);

        MethodInfo? renderCoreMenuToggle = typeof(MenuSystem).GetMethod(nameof(MenuSystem.RenderCoreMenuToggle), NonPublic | Static);

        if (renderCoreMenuToggle is not null)
            PatchRenderCoreMenuToggle = new(renderCoreMenuToggle,
                ModifyCoreToggle);

        MethodInfo? drawButton = typeof(MenuSystem).GetMethod(nameof(MenuSystem.DrawButton), NonPublic | Static);

        if (drawButton is not null)
            PatchDrawButton = new(drawButton,
                ModifyButtons);
    }

    public override void Unload()
    {
        PatchRenderVanillaMenuToggle?.Dispose();
        PatchRenderCoreMenuToggle?.Dispose();

        PatchDrawButton?.Dispose();
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

                // Likely unecessary.
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

                // Match to the color of the menu switch text.
            c.GotoNext(MoveType.After,
                i => i.MatchLdloca(out _),
                i => i.MatchLdsfld<Main>(nameof(Main.mouseX)),
                i => i.MatchLdsfld<Main>(nameof(Main.mouseY)),
                i => i.MatchCall<Rectangle>(nameof(Rectangle.Contains)));

            c.EmitDelegate((bool hovering) => hovering && !Hovering);

                // Match to after the menu switch text is drawn.
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

            int hoveringIndex = -1; // arg-intpointer

            int colorIndex = -1;

            c.GotoNext(MoveType.After,
                i => i.MatchLdarg(out hoveringIndex),
                i => i.MatchLdloca(out _),
                i => i.MatchCall<Main>($"get_{nameof(Main.MouseScreen)}"),
                i => i.MatchCall(typeof(Utils).FullName ?? "Terraria.Utils", nameof(Utils.ToPoint)),
                i => i.MatchCall<Rectangle>(nameof(Rectangle.Contains)),
                i => i.MatchStindI1());

            c.EmitLdarg(hoveringIndex);

            c.EmitLdarg(hoveringIndex);
            c.EmitLdindI1();

            c.EmitDelegate((bool hovering) =>
                hovering && !Hovering);

            c.EmitStindI1();

            c.GotoNext(MoveType.After,
                i => i.MatchLdsfld<Main>(nameof(Main.OurFavoriteColor)),
                i => i.MatchStloc(out colorIndex));

            c.MoveAfterLabels();

            c.EmitLdarg(hoveringIndex);
            c.EmitLdindI1();

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
