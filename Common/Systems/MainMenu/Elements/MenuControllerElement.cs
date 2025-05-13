using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;

namespace ZensSky.Common.Systems.MainMenu.Elements;

public abstract class MenuControllerElement : UIPanel, ILoadable
{
    public UIText? UIName;

    public abstract int Index { get; }

    public abstract string Name { get; }

    public void Load(Mod mod) => MenuControllerSystem.Controllers.Add(this);

    public void Unload() { }

    public MenuControllerElement()
    {
        UIName = new(Name)
        {
            HAlign = 0.5f
        };

        Append(UIName);
    }

    public override int CompareTo(object obj)
    {
        if (obj is MenuControllerElement element)
            return element.Index > Index ? -1: 1;
        return 0;
    }
}
