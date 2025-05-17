using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader.UI;
using ZensSky.Common.Config;
using ZensSky.Common.Registries;

namespace ZensSky.Common.Systems.MainMenu.Elements;

public sealed class SunAndMoonStyle : MenuControllerElement
{
    public override int Index => 1;

    public override string Name => "Mods.ZensSky.MenuController.SunAndMoonStyle";

    public SunAndMoonStyle() : base()
    {
        Height.Set(190f, 0f);

        UIImageButton eclipse = new(Textures.LockedToggle);

        eclipse.SetHoverImage(Textures.ButtonHover);

        eclipse.Width.Set(14f, 0f);
        eclipse.Height.Set(14f, 0f);

            // eclipse.OnMouseOver

        Append(eclipse);
    }

    public override void OnLoad()
    {
        
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

    }
}
