using Microsoft.Xna.Framework.Graphics;

namespace ZensSky.Common.DataStructures;

public record struct SkyLightInfo
{
    #region Public Properties

    public Color Color { get; set; }

    public Vector2 Position { get; set; }

    public float Size { get; set; }

    public Texture2D? Texture { get; set; }

    #endregion

    #region Public Constructors

    public SkyLightInfo(Color color, Vector2 position, float size, Texture2D? texture = null)
    {
        Color = color;
        Position = position;
        Size = size;
        Texture = texture;
    }

    #endregion
}
