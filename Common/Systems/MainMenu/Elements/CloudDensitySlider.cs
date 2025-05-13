using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.GameContent.UI.Elements;

namespace ZensSky.Common.Systems.MainMenu.Elements;

public sealed class CloudDensitySlider : MenuControllerElement
{
    public override int Index => 1;

    public override string Name => "TestClouds";

    public CloudDensitySlider() : base()
    {
        Height.Set(80f, 0f);
    }
}
