using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using ReLogic.Graphics;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace ZensSky.Common.Systems.MainMenu;

public sealed class AddPopupSystem : ModSystem
{
    #region Private Fields

    private static readonly Color NotHovered = new(120, 120, 120, 76);
    private const int HorizontalPadding = 4;

    private static ILHook? AddPopupButton;

    private static readonly UserInterface PopupInterface = new();
    private static readonly PopupUIState Popup = new();

    private static bool InUI => PopupInterface?.CurrentState is not null;

    #endregion

    #region Loading

    public override void Load()
    {
        MethodInfo? updateAndDrawModMenuInner = typeof(MenuLoader).GetMethod("UpdateAndDrawModMenuInner", BindingFlags.Static | BindingFlags.NonPublic);

        if (updateAndDrawModMenuInner is not null)
            AddPopupButton = new(updateAndDrawModMenuInner, AddPopup);

        IL_Main.DrawMenu += ModifyInteraction;
        Main.OnResolutionChanged += CloseMenuOnResolutionChanged;

        Popup?.Activate();
    }

    public override void Unload()
    {
        AddPopupButton?.Dispose();
        IL_Main.DrawMenu -= ModifyInteraction;
        Main.OnResolutionChanged -= CloseMenuOnResolutionChanged;
    }

    #endregion

    private void AddPopup(ILContext il)
    {
        ILCursor c = new(il);

            // Match to before the menu switch text is drawn.
        if (!c.TryGotoNext(MoveType.After,
            i => i.MatchCall("Terraria.ModLoader.MenuLoader", "OffsetModMenu"),
            i => i.MatchLdsfld<Main>("menuMode"),
            i => i.MatchBrtrue(out _)))
            throw new ILPatchFailureException(Mod, il, null);

        c.EmitLdarg0(); // SpriteBatch.
        c.EmitLdloc(6); // Rectangle of the menu button.

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
                PopupInterface?.SetState(InUI ? null : Popup);
                Popup.Bottom = new(popupRect.Center.X, position.Y);

                    // Reinit to update the position.
                Popup?.OnInitialize();
                SoundEngine.PlaySound(SoundID.MenuTick);
            }
        });
    }

    private void ModifyInteraction(ILContext il)
    {
        ILCursor c = new(il);

            // Update our UI.
        c.EmitDelegate(() =>
        {
            if (InUI)
            {
                if (Main.menuMode == 0)
                    PopupInterface?.Update(new GameTime());
                else
                    PopupInterface?.SetState(null);
            }
        });

        if (!c.TryGotoNext(i => i.MatchLdloc(173)))
            throw new ILPatchFailureException(Mod, il, null);

            // Genuinely I cant.
        string[] names = ["focusMenu", "selectedMenu", "selectedMenu2"];
        for (int j = 0; j < names.Length * 2; j++)
        {
            if (c.TryGotoNext(MoveType.Before, i => i.MatchStfld<Main>(names[j % names.Length])))
                c.EmitDelegate((int num98) => InUI && Popup?.Panel?.IsMouseHovering is true ? -1 : num98);
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
                PopupInterface?.Draw(Main.spriteBatch, new GameTime());
        });
    }

    private void CloseMenuOnResolutionChanged(Vector2 obj) => PopupInterface?.SetState(null);
}
