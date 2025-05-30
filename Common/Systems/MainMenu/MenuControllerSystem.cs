using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using ReLogic.Graphics;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.UI;
using Terraria.UI.Chat;
using ZensSky.Common.Config;
using ZensSky.Common.Systems.MainMenu.Elements;
using static System.Reflection.BindingFlags;

namespace ZensSky.Common.Systems.MainMenu;

[Autoload(Side = ModSide.Client)]
public sealed class MenuControllerSystem : ModSystem
{
    #region Private Fields

    private static readonly Color NotHovered = new(120, 120, 120, 76);
    private const int HorizontalPadding = 4;

    private static ILHook? AddMenuControllerToggle;

    private delegate void orig_Save(ModConfig config);
    private static Hook? SaveConfig;

    private static readonly UserInterface MenuControllerInterface = new();
    private static readonly MenuControllerUIState MenuController = new();

    private static bool InUI => MenuControllerInterface?.CurrentState is not null;

    #endregion

    public static readonly List<MenuControllerElement> Controllers = [];

    #region Loading

    public override void Load()
    {
        Main.QueueMainThreadAction(() =>
        {
            MethodInfo? updateAndDrawModMenuInner = typeof(MenuLoader).GetMethod("UpdateAndDrawModMenuInner", Static | NonPublic);

            if (updateAndDrawModMenuInner is not null)
                AddMenuControllerToggle = new(updateAndDrawModMenuInner, 
                    AddToggle);

            MethodInfo? save = typeof(ConfigManager).GetMethod("Save", Static | NonPublic);

            if (save is not null)
                SaveConfig = new(save,
                    RefreshOnSave);

            IL_Main.DrawMenu += ModifyInteraction;
            On_Main.UpdateUIStates += UpdateInterface;
            Main.OnResolutionChanged += CloseMenuOnResolutionChanged;
        });

        MenuController?.Activate();
    }

    public override void Unload()
    {
        AddMenuControllerToggle?.Dispose();
        Main.QueueMainThreadAction(() =>
        {
            IL_Main.DrawMenu -= ModifyInteraction;
            On_Main.UpdateUIStates -= UpdateInterface;
            Main.OnResolutionChanged -= CloseMenuOnResolutionChanged;
        });
    }

    #endregion

    public static void RefreshAll() => Controllers.ForEach((controller) => { controller.Refresh(); });

    #region Toggle Button

    private void AddToggle(ILContext il)
    {
        ILCursor c = new(il);

            // Match to before the menu switch text is drawn.
        if (!c.TryGotoNext(MoveType.After,
            i => i.MatchCall("Terraria.ModLoader.MenuLoader", "OffsetModMenu"),
            i => i.MatchLdsfld<Main>("menuMode"),
            i => i.MatchBrtrue(out _)))
            throw new ILPatchFailureException(Mod, il, null);

        c.EmitLdarg0(); // SpriteBatch.
        c.EmitLdloc(6); // Rectangle of the menu switcher.

            // Add our own 'popup' menu button.
        c.EmitDelegate((SpriteBatch spriteBatch, Rectangle switchTextRect) =>
        {
            Vector2 position = switchTextRect.TopRight();
            position.X += HorizontalPadding;

            DynamicSpriteFont font = FontAssets.MouseText.Value;
            string text = InUI ? "▲" : "▼";

            Vector2 size = ChatManager.GetStringSize(font, text, Vector2.One);

            Rectangle popupRect = new((int)position.X, (int)position.Y, 
                (int)size.X, (int)size.Y);

            bool hovering = popupRect.Contains(Main.mouseX, Main.mouseY) && !Main.alreadyGrabbingSunOrMoon;

            Color color = hovering ? Main.OurFavoriteColor : NotHovered;

            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, position, color, 0f, Vector2.Zero, Vector2.One);

            if (hovering && Main.mouseLeft && Main.mouseLeftRelease)
            {
                if (InUI)
                    ConfigManager.Save(MenuConfig.Instance);

                MenuControllerInterface?.SetState(InUI ? null : MenuController);
                MenuController.Bottom = new(popupRect.Center.X, position.Y);

                    // Reinit for easy debugging.
                MenuController?.OnInitialize();
                SoundEngine.PlaySound(SoundID.MenuTick);
            }
        });
    }

    #endregion

    #region Updating

    private void RefreshOnSave(orig_Save orig, ModConfig config)
    {
        orig(config);

        if (config is MenuConfig)
            RefreshAll();
    }

        // For whatever reason ModSystem::UpdateUI does not run on the titlescreen ???
    private void UpdateInterface(On_Main.orig_UpdateUIStates orig, GameTime gameTime)
    {
        if (InUI)
        {
            if (Main.menuMode == 0)
                MenuControllerInterface?.Update(gameTime);
            else
            {
                MenuControllerInterface?.SetState(null);
                ConfigManager.Save(MenuConfig.Instance);
            }
        }

        orig(gameTime);
    }

    public override void OnWorldUnload()
    {
        for (int i = 0; i < Controllers.Count; i++)
            Controllers[i].Refresh();
    }

    private void ModifyInteraction(ILContext il)
    {
        ILCursor c = new(il);

        if (!c.TryGotoNext(i => i.MatchLdloc(173)))
            throw new ILPatchFailureException(Mod, il, null);

            // Genuinely I can't.
        string[] names = ["focusMenu", "selectedMenu", "selectedMenu2"];
        for (int j = 0; j < names.Length * 2; j++)
        {
            if (c.TryGotoNext(MoveType.Before, i => i.MatchStfld<Main>(names[j % names.Length])))
                c.EmitDelegate((int num98) => InUI && MenuController?.Panel?.IsMouseHovering is true ? -1 : num98);
        }

            // Have our popup draw.
        if (!c.TryGotoNext(MoveType.AfterLabel, // Rare usage.
            i => i.MatchLdloc2(),
            i => i.MatchLdloc(30),
            i => i.MatchCall<Main>("DrawtModLoaderSocialMediaButtons")))
            throw new ILPatchFailureException(Mod, il, null);

        c.EmitDelegate(() => 
        {
            if (InUI)
                MenuControllerInterface?.Draw(Main.spriteBatch, new GameTime());
        });
    }

    private void CloseMenuOnResolutionChanged(Vector2 obj) 
    {
        MenuControllerInterface?.SetState(null);
        ConfigManager.Save(MenuConfig.Instance);
    }

    #endregion
}
