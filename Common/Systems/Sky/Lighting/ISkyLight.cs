using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace ZensSky.Common.Systems.Sky.Lighting;

public interface ISkyLight
{
    #region Public Properties

    public bool Active { get; }

    public Color Color { get; }

    public Vector2 Position { get; }

    public float Size { get; }

    public Asset<Texture2D>? Texture => null;

    #endregion
}
