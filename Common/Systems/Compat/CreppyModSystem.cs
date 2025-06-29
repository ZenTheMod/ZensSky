using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using static ZensSky.Common.Registries.Textures;

namespace ZensSky.Common.Systems.Compat;

[JITWhenModsEnabled("CreppyMod")]
[ExtendsFromMod("CreppyMod")]
[Autoload(Side = ModSide.Client)]
public sealed class CreppyModSystem : ModSystem
{
    #region Private Fields

    private const float CreppySunScale = .25f;

    #endregion

    #region Public Properties

    public static bool IsEnabled { get; private set; }

    public static bool CreppyModeOn => CreppyMode.CreppyModeOn;

    #endregion

    #region Loading

    public override void Load() =>
        IsEnabled = true;

    #endregion

    #region Public Methods

    public static void DrawCreppySun(SpriteBatch spriteBatch, Vector2 position, float rotation, float scale)
    {
        if (!CreppyModeOn) 
            return;

        Texture2D sun = CreppySun.Value;
        spriteBatch.Draw(sun, position, null, Color.White, rotation, sun.Size() * .5f, CreppySunScale * scale, SpriteEffects.None, 0f);
    }

    #endregion
}
