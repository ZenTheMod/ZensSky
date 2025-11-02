using Daybreak.Common.Features.Authorship;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;
using static ZensSky.GeneratedAssets.Textures.Authorship.Textures;

namespace ZensSky.Common.Authorship;

public abstract class ZensSkyAuthorTag : AuthorTag
{
    #region Private Fields

    private const string TagSuffix = "Tag";

    #endregion

    #region Public Properties

    public override string Name =>
        base.Name.EndsWith(TagSuffix) ? base.Name.Replace(TagSuffix, string.Empty) : base.Name;

    public override string Texture =>
        string.Join('/', Sprunolia.Key.Split('/', '\\')[..^1]) + $"/{Name}";

    #endregion

    #region Drawing

    public override void DrawIcon(SpriteBatch spriteBatch, Vector2 position)
    {
        if (!ModContent.RequestIfExists(Texture, out Asset<Texture2D> icon))
            return;

            // Use 32x sprites over 26x ones.
        Rectangle rectangle = new((int)position.X - 4, (int)position.Y - 6, 32, 32);

        spriteBatch.Draw(icon.Value, rectangle, Color.White);
    }

    #endregion
}
