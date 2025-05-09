using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace ZensSky.Common.Systems.MainMenu;

public sealed class PopupUIState : UIState
{
    public Vector2 Bottom;

    public UIPanel? Panel;
    public UIList? Options;

    public override void OnInitialize()
    {
        RemoveAllChildren();

        Panel = new();

        Panel.Width.Set(400f, 0f);

        Panel.Height.Set(600f, 0f);

        Panel.Top.Set(Bottom.Y - 600, 0f);
        Panel.Left.Set(Bottom.X - 200, 0f);

        Append(Panel);
    }

    public override void Update(Microsoft.Xna.Framework.GameTime gameTime)
    {
        base.Update(gameTime);
    }
}
