using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ZensSky.Common.Systems.MainMenu.Elements;

public abstract class MenuControllerElement : UIPanel, ILoadable
{
    public UIText? UIName;

    public abstract int Index { get; }

    public abstract string Name { get; }

    public virtual void OnLoad() { }

    public void Load(Mod mod) 
    { 
        MenuControllerSystem.Controllers.Add(this); 
        OnLoad();
    }

    public void Unload() { }

    public MenuControllerElement()
    {
        UIName = new(Language.GetText(Name))
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
