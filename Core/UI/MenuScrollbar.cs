using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;
using ZensSky.Core.Utils;

namespace ZensSky.Core.UI;

public sealed class MenuScrollbar : UIScrollbar
{
    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        CalculatedStyle dimensions = GetDimensions();
        CalculatedStyle innerDimensions = GetInnerDimensions();

        Vector2 mousePosition = Utilities.UIMousePosition;

        if (_isDragging)
        {
            float position = mousePosition.Y - innerDimensions.Y - _dragYOffset;
            _viewPosition = MathHelper.Clamp(position / innerDimensions.Height * _maxViewSize, 0f, _maxViewSize - _viewSize);
        }

        Rectangle handleRectangle = GetHandleRectangle();
        bool isHoveringOverHandle = _isHoveringOverHandle;

        _isHoveringOverHandle = handleRectangle.Contains(mousePosition.ToPoint()) && !Main.alreadyGrabbingSunOrMoon;

        if (!isHoveringOverHandle && _isHoveringOverHandle && Main.hasFocus)
            SoundEngine.PlaySound(SoundID.MenuTick);

        DrawBar(spriteBatch, _texture.Value, dimensions.ToRectangle(), Color.White);
        DrawBar(spriteBatch, _innerTexture.Value, handleRectangle, Color.White * (_isDragging || _isHoveringOverHandle ? 1f : 0.85f));
    }

    public override void LeftMouseDown(UIMouseEvent evt)
    {
        if (!Main.alreadyGrabbingSunOrMoon)
            base.LeftMouseDown(evt);
    }
}
