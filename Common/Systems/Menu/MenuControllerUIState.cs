using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader.Config;
using Terraria.UI;
using ZensSky.Common.Config;
using ZensSky.Common.Registries;
using ZensSky.Common.Systems.Menu.Elements;

namespace ZensSky.Common.Systems.Menu;

public sealed class MenuControllerUIState : UIState
{
    #region Private Fields

    private const float VerticalGap = 5f;

    private const string Header = "Mods.ZensSky.MenuController.Header";
    private const float HeaderHeight = 30f;

    private const string ResetTooltip = "Mods.ZensSky.MenuController.ResetTooltip";

    #endregion

    #region Public Fields

    public Vector2 Bottom;

    public UIPanel? Panel;
    public UIList? Controllers;

    public FixedImageButton? ResetButton;

    #endregion

    #region Initialization

    public override void OnInitialize()
    {
        RemoveAllChildren();

        CalculatedStyle dims = GetDimensions();

            // Setup the container panel.
        Panel = new();

        Panel.Width.Set(400f, 0f);
        Panel.MaxWidth.Set(0f, 0.8f);
        Panel.MinWidth.Set(300f, 0f);

        Panel.Height.Set(500f, 0f);
        Panel.MaxHeight.Set(0f, 0.7f);
        Panel.MinHeight.Set(200f, 0f);

        Panel.Top.Set(Bottom.Y - Panel.Height.GetValue(dims.Height) - VerticalGap, 0f);
        Panel.Left.Set(Bottom.X - Panel.Width.GetValue(dims.Width) * 0.5f, 0f);

        Append(Panel);

        UIText header = new(Language.GetText(Header), 0.5f, true)
        {
            HAlign = 0.5f
        };

        Panel.Append(header);

            // Add a reset button to quickly reset the config without having to even open it.
        ResetButton = new(Textures.Reset)
        {
            HAlign = 1f
        };

        ResetButton.Width.Set(24f, 0f);
        ResetButton.Height.Set(20f, 0f);

        ResetButton.OnLeftMouseDown += ClickReset;

        Panel.Append(ResetButton);

            // Setup the controller list.
        Controllers = [];

        Controllers.Width.Set(-25f, 1f);
        Controllers.Height.Set(-HeaderHeight, 1f);

        Controllers.Top.Set(HeaderHeight, 0f);

        Panel.Append(Controllers);

            // Use our modified scrollbar to prevent hovering while grabbing the sun or moon.
        FixedScrollbar uIScrollbar = new();

        uIScrollbar.SetView(100f, 1000f); // This seems to be important ?
        uIScrollbar.Height.Set(-HeaderHeight, 1f);
        uIScrollbar.HAlign = 1f;

        uIScrollbar.Top.Set(HeaderHeight, 0f);

        Panel.Append(uIScrollbar);

        Controllers.SetScrollbar(uIScrollbar);

            // Add list items.
        List<MenuControllerElement> controllers = MenuControllerSystem.Controllers;
        for (int i = 0; i < controllers.Count; i++)
        {
            controllers[i].Width.Set(0f, 1f);
            Controllers.Add(controllers[i]);
        }

        Recalculate();
    }

    #endregion

    #region Updating

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (ResetButton?.IsMouseHovering is not true)
            return;

        string tooltip = Language.GetTextValue(ResetTooltip);
        Main.instance.MouseText(tooltip);
    }

    #endregion

    #region Private Methods

    private void ClickReset(UIMouseEvent evt, UIElement listeningElement)
    {
        ConfigManager.Reset(MenuConfig.Instance);
        MenuControllerSystem.RefreshAll();

        SoundEngine.PlaySound(SoundID.MenuOpen);
    }

    #endregion
}
