using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using Terraria;
using Terraria.ModLoader.Config.UI;
using Terraria.ModLoader.UI;
using Terraria.UI;
using ZensSky.Core.Utils;

namespace ZensSky.Core.Config.Elements;

[UseSplitPanel]
public abstract class DropDownConfigElement<T> : ConfigElement<T>
{
    #region Private Fields

    protected const float BaseHeight = 30f;

    #endregion

    #region Public Properties

    public bool MenuOpen
    {
        get => field;
        set
        {
            field = value;

            Height.Set(value ? ExpandedHeight : BaseHeight, 0f);

                // Remember to set the parent wrapper's height.
            Parent.Height.Set(value ? ExpandedHeight : BaseHeight, 0f);

            Recalculate();

            if (value)
                OnExpand();
            else
                OnContract();
        }
    }

    public abstract float ExpandedHeight { get; }

    public bool HoveringTop { get; private set; }

    #endregion

    #region Interactions

    protected virtual void OnExpand() { }

    protected virtual void OnContract() =>
        RemoveAllChildren();

    public override void LeftMouseDown(UIMouseEvent evt)
    {
        base.LeftMouseDown(evt);

        if (HoveringTop)
            MenuOpen = !MenuOpen;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        Rectangle dims = this.Dimensions;

        HoveringTop = IsMouseHovering &&
            Utilities.UIMousePosition.Y < dims.Y + BaseHeight;

        bool anyMouseInput =
            Main.mouseLeft
            || Main.mouseMiddle
            || Main.mouseRight
            || Main.mouseXButton1
            || Main.mouseXButton2;

            // Not pretty.
        if (Main.hasFocus &&
            MenuOpen &&
            IsMouseHovering &&
            !HoveringTop &&
            anyMouseInput &&
            Children.Any(e => e.IsMouseHovering))
        {
            MemberInfo.SetValue(Item, Value);
            Interface.modConfig.SetPendingChanges();
        }
    }

    #endregion

    #region Drawing

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        bool prior = IsMouseHovering;

        IsMouseHovering = HoveringTop;

        base.DrawSelf(spriteBatch);

        IsMouseHovering = prior;
    }

    #endregion
}
