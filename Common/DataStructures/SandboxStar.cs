using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Utilities;
using ZensSky.Common.Registries;
using ZensSky.Core;
using ZensSky.Core.Utils;

namespace ZensSky.Common.DataStructures;

public record struct SandboxStar : ISpatial
{
    #region Private Fields

    private const float MinSize = .1f;
    private const float MaxSize = .45f;

    private static readonly Vector2 BoundsSize = new(10);

    #endregion

    #region Public Properties

    public required Vector2 Position { get; set; }

    public required Vector2 Velocity { get; set; }

    public required float Size { get; init; }

    public readonly Rectangle Bounds =>
        Utils.CenteredRectangle(Position, BoundsSize);

    #endregion

    #region Drawing

    public readonly void Draw(SpriteBatch spriteBatch)
    {
        Texture2D texture = Textures.OuterWildsStar.Value;
        Vector2 origin = texture.Size() * .5f;

        Vector2 position = Position;

        Color color = Color.White;
        color.A = 0;

        float scale = Size;

        spriteBatch.Draw(texture, position, null, color, 0, origin, scale, SpriteEffects.None, 0f);
    }

    #endregion

    public static SandboxStar Create(UnifiedRandom rand) => new()
    {
        Position = rand.NextVector2FromRectangle(Utilities.ScreenDimensions),
        Velocity = Vector2.Zero,
        Size = rand.NextFloat(MinSize, MaxSize)
    };
}
