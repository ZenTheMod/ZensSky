﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Terraria.UI;
using Terraria.UI.Chat;
using ZensSky.Common.Config;
using ZensSky.Common.Systems.Menu.Elements;
using ZensSky.Core.Exceptions;
using ZensSky.Core.Systems;
using static System.Reflection.BindingFlags;

namespace ZensSky.Common.Systems.Menu;

[Autoload(Side = ModSide.Client)]
public sealed class MenuControllerSystem : ModSystem
{
    #region Private Fields

    private static readonly Color NotHovered = new(120, 120, 120, 76);
    private const int HorizontalPadding = 4;

    private static ILHook? AddMenuControllerToggle;

    private static ILHook? HideConfigFromList;

    private delegate void orig_Save(ModConfig config);
    private static Hook? SaveConfig;

    private static readonly UserInterface MenuControllerInterface = new();
    private static readonly MenuControllerUIState MenuController = new();

    #endregion

    #region Public Properties

    public static bool InUI => MenuControllerInterface?.CurrentState is not null;

    public static bool Hovering => InUI && MenuController?.Panel?.IsMouseHovering is true;

    #endregion

    #region Public Fields

    public static readonly List<MenuControllerElement> Controllers = [];

    #endregion

    #region Loading

    public override void Load()
    {
        MainThreadSystem.Enqueue(() =>
        {
            MethodInfo? updateAndDrawModMenuInner = typeof(MenuLoader).GetMethod(nameof(MenuLoader.UpdateAndDrawModMenuInner), Static | NonPublic);

            if (updateAndDrawModMenuInner is not null)
                AddMenuControllerToggle = new(updateAndDrawModMenuInner, 
                    AddToggle);

            IL_Main.DrawMenu += ModifyInteraction;
            On_Main.UpdateUIStates += UpdateInterface;
            Main.OnResolutionChanged += CloseMenuOnResolutionChanged;
        });

        MethodInfo? populateConfigs = typeof(UIModConfigList).GetMethod(nameof(UIModConfigList.PopulateConfigs), Instance | NonPublic);

        if (populateConfigs is not null)
            HideConfigFromList = new(populateConfigs,
                HideMenuConfig);

        MethodInfo? save = typeof(ConfigManager).GetMethod(nameof(ConfigManager.Save), Static | NonPublic);

        if (save is not null)
            SaveConfig = new(save,
                RefreshOnSave);

        MenuController?.Activate();
    }

    public override void Unload()
    {
        MainThreadSystem.Enqueue(() =>
        {
            AddMenuControllerToggle?.Dispose();

            SaveConfig?.Dispose();

            IL_Main.DrawMenu -= ModifyInteraction;
            On_Main.UpdateUIStates -= UpdateInterface;
            Main.OnResolutionChanged -= CloseMenuOnResolutionChanged;
        });

        HideConfigFromList?.Dispose();

        SaveConfig?.Dispose();
    }

    public override void PostSetupContent() =>
        RefreshAll();

    #endregion

    #region Public Methods

    public static void RefreshAll() => Controllers.ForEach((controller) => { controller.Refresh(); });

    #endregion

    #region Menu Additions

    private void AddToggle(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

                // Match to before the menu switch text is drawn.
            c.GotoNext(MoveType.After,
                i => i.MatchCall(typeof(MenuLoader).FullName ?? "Terraria.ModLoader.MenuLoader", nameof(MenuLoader.OffsetModMenu)),
                i => i.MatchLdsfld<Main>(nameof(Main.menuMode)),
                i => i.MatchBrtrue(out _));

            c.EmitLdarg0(); // SpriteBatch.
            c.EmitLdloc(6); // Rectangle of the menu switcher.

                // Add our own 'popup' menu button.
            c.EmitDelegate((SpriteBatch spriteBatch, Rectangle switchTextRect) =>
            {
                Vector2 position = switchTextRect.TopRight();
                position.X += HorizontalPadding;

                DynamicSpriteFont font = FontAssets.MouseText.Value;
                string text = InUI ? "▼" : "▲";

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
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    private void ModifyInteraction(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

                // TODO: Match for something better.
            c.GotoNext(i => i.MatchLdloc(173));

                // Genuinely I can't.
            string[] names = [nameof(Main.focusMenu), nameof(Main.selectedMenu), nameof(Main.selectedMenu2)];
            for (int j = 0; j < names.Length * 2; j++)
            {
                if (c.TryGotoNext(MoveType.Before, i => i.MatchStfld<Main>(names[j % names.Length])))
                    c.EmitDelegate((int hovering) => Hovering ? -1 : hovering);
            }

                // Have our popup draw.
            c.TryGotoNext(MoveType.AfterLabel,
                i => i.MatchLdloc(out _),
                i => i.MatchLdloc(out _),
                i => i.MatchCall<Main>(nameof(Main.DrawtModLoaderSocialMediaButtons)));

            c.EmitDelegate(() =>
            {
                if (InUI)
                    MenuControllerInterface?.Draw(Main.spriteBatch, new GameTime());
            });
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    #endregion

    #region Config

    private void HideMenuConfig(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            ILLabel? addConfigSkipTarget = c.DefineLabel();

            int configIndex = -1;

                // Not exactly sure how to use a compiler generated backing class as a type argument.
            FieldReference? displayClassLocal = null;
            FieldReference? displayClassConfig = null;

                // Get the label to the bottom of the loop, this will act as our 'continue' keyword.
            c.GotoNext(MoveType.After,
                i => i.MatchCallvirt<List<ModConfig>>(nameof(List<>.GetEnumerator)),
                i => i.MatchStloc(out _),
                i => i.MatchBr(out addConfigSkipTarget));

            c.GotoNext(MoveType.After,
                i => i.MatchLdloc(out configIndex),
                i => i.MatchLdfld(out displayClassLocal),
                i => i.MatchLdfld(out displayClassConfig),
                i => i.MatchCallvirt<ModConfig>($"get_{nameof(ModConfig.DisplayName)}"));

            if (displayClassLocal is null || displayClassConfig is null)
                throw new NullReferenceException();

            c.GotoPrev(MoveType.After,
                i => i.MatchStloc(configIndex),
                i => i.MatchLdloc(configIndex),
                i => i.MatchLdloc(out _),
                i => i.MatchStfld(displayClassLocal));

            c.EmitLdloc(configIndex);

            c.EmitLdfld(displayClassLocal);
            c.EmitLdfld(displayClassConfig);

            c.EmitDelegate((ModConfig config) => config is MenuConfig);

            c.EmitBrtrue(addConfigSkipTarget);
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    private void RefreshOnSave(orig_Save orig, ModConfig config)
    {
        orig(config);

        if (config is MenuConfig)
            RefreshAll();
    }

        // For whatever reason ModSystem.UpdateUI does not run on the titlescreen ???
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

    private void CloseMenuOnResolutionChanged(Vector2 obj)
    {
        MenuControllerInterface?.SetState(null);
        ConfigManager.Save(MenuConfig.Instance);
    }

    public override void OnWorldUnload() => RefreshAll();

    #endregion
}
