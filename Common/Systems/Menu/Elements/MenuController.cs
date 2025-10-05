using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ZensSky.Common.Systems.Menu.Elements;

[Autoload(Side = ModSide.Client)]
public abstract class MenuController : UIPanel, ILoadable
{
    #region Public Fields

    public UIText? UIName;

    #endregion

    #region Public Properties

    public abstract int Index { get; }

    public abstract string Name { get; }

    #endregion

    #region Loading

    public virtual void OnLoad() { }

    public virtual void OnUnload() { }

    void ILoadable.Load(Mod mod) 
    { 
        MenuControllerSystem.Controllers.Add(this); 
        OnLoad();
        Refresh();
    }

    void ILoadable.Unload() =>
        OnUnload();

    public virtual void Refresh() { }

    #endregion

    #region Public Constructors

    public MenuController()
    {
        UIName = new(Language.GetText(Name))
        {
            HAlign = 0.5f
        };

        Append(UIName);
    }

    #endregion

    #region Sorting

    public override int CompareTo(object obj)
    {
        if (obj is MenuController element)
            return element.Index > Index ? -1: 1;
        return 0;
    }

    #endregion
}
