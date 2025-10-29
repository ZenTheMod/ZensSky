using Microsoft.Xna.Framework.Graphics;
using ZensSky.Common.DataStructures;


namespace ZensSky.Common.Systems.Sky.Lighting;

public abstract class SkyLight
{
    #region Public Delegates

    public delegate void hook_OnGetInfo(ref SkyLightInfo info);

    #endregion

    #region Public Events

    public event hook_OnGetInfo? OnGetInfo;

    #endregion

    #region Public Properties

    public virtual bool Active => true;

    #endregion

    #region Private Properties

    protected abstract Color Color { get; }

    protected abstract Vector2 Position { get; }

    protected abstract float Size { get; }

    protected virtual Texture2D? Texture => null;

    #endregion

    #region Public Methods

    public SkyLightInfo GetInfo()
    {
        SkyLightInfo info = new(Color, Position, Size, Texture);

        OnGetInfo?.Invoke(ref info);

        return info;
    }

    #endregion
}
