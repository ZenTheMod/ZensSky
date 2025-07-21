using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.DataStructures;
using ZensSky.Core.DataStructures;
using ZensSky.Core.Utils;

namespace ZensSky.Common.MenuStyles;

[Autoload(Side = ModSide.Client)]
public sealed class StarSandbox : ModMenu
{
    #region Private Fields

    private const float DetectionRange = 40f;

    private const int StarCount = 600;
    private static readonly SandboxStar[] Stars = new SandboxStar[StarCount];

    #endregion

    public override void OnSelected()
    {
        for (int i = 0; i < StarCount; i++)
            Stars[i] = SandboxStar.Create(Main.rand);
    }

    #region Updating

    public override void Update(bool isOnTitleScreen) =>
        UpdateStars();

    private static void UpdateStars()
    {
        QuadTree<SandboxStar> starTree = new(Utilities.ScreenDimensions, 0);

        starTree.Insert(Stars);

        for (int i = 0; i < StarCount; i++)
        {
            HashSet<SandboxStar> near = starTree.Query(Utils.CenteredRectangle(Stars[i].Position, new(DetectionRange * 2f)), Stars[i]);

            Stars[i].Velocity *= .9f;

            if (near.Count > 0)
            {
                SandboxStar nearest = near.CompareFor(s => s.Position.DistanceSQ(Stars[i].Position), false);

                Stars[i].Velocity += Utils.SafeNormalize(Stars[i].Position - nearest.Position, Vector2.Zero) *
                    (DetectionRange - nearest.Position.Distance(Stars[i].Position)) * .05f;
            }

            if (Stars[i].Position.Distance(Utilities.MousePosition) < 120) 
            {
                Stars[i].Velocity += Utils.SafeNormalize(Stars[i].Position - Utilities.MousePosition, Vector2.Zero) *
                    (120 - Utilities.MousePosition.Distance(Stars[i].Position)) * .02f;
            }

            Stars[i].Position += Stars[i].Velocity;

            Stars[i].Position = Vector2.Clamp(Stars[i].Position, Vector2.Zero, Utilities.ScreenSize);
        }
    }

    #endregion

    public override bool PreDrawLogo(SpriteBatch spriteBatch, ref Vector2 logoDrawCenter, ref float logoRotation, ref float logoScale, ref Color drawColor)
    {
        Array.ForEach(Stars, s => s.Draw(spriteBatch));

        return true;
    }
}
