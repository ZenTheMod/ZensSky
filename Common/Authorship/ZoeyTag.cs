using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace ZensSky.Common.Authorship;

public sealed class ZoeyTag : ZensSkyAuthorTag
{
    #region Private Fields

    private static readonly Color GlowColor = new(179, 133, 255);

    #endregion

    #region Drawing

    public override void DrawIcon(SpriteBatch spriteBatch, Vector2 position)
    {
        Vector2 glowPosition = new((int)position.X - 4, (int)position.Y - 6);

        Color glowColor = GlowColor * MathF.Sin(Main.GlobalTimeWrappedHourly);

        spriteBatch.Draw(AuthorshipTextures.ZoeyGlow, glowPosition, glowColor);

        base.DrawIcon(spriteBatch, position);
    }

    #endregion
}
