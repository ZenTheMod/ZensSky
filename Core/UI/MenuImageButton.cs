using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace ZensSky.Core.UI;

public sealed class MenuImageButton(Asset<Texture2D> texture) : UIImageButton(texture)
{
    public override void MouseOver(UIMouseEvent evt)
    {
        base.MouseOver(evt);

        IsMouseHovering = !Main.alreadyGrabbingSunOrMoon;

        if (!IsMouseHovering)
            return;

        SoundEngine.PlaySound(SoundID.MenuTick);
    }
}
