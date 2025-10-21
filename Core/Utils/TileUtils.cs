using Terraria;
using Terraria.ID;

namespace ZensSky.Core.Utils;

    // The C# 14.0 'extension' block seems to still be a little buggy.
#pragma warning disable CA1822 // Member does not access instance data and can be marked as static.

public static partial class Utilities
{
    public static bool ShowInvisibleTiles =>
        Main.instance.TilesRenderer._shouldShowInvisibleBlocks;

    extension(Tile tile)
    {
        public bool HasSolidTile =>
            tile.HasTile &&
            Main.tileBlockLight[tile.type] &&
            Main.tileSolid[tile.type] && // Potentially stupid.
            tile.IsFullBlock &&
            (!tile.IsTileInvisible || ShowInvisibleTiles);

        /// <summary>
        /// Whether there is a wall at this position.<br/>
        /// Shorthand for <c>tile.WallType != WallID.None</c>.
        /// </summary>
        public bool HasWall =>
            tile.WallType != WallID.None;

        public bool HasSolidWall =>
            tile.HasWall && !Main.wallLight[tile.WallType] && !WallID.Sets.Transparent[tile.WallType] &&
            (!tile.IsWallInvisible || ShowInvisibleTiles);

        public bool IsAir =>
            !tile.HasTile && !tile.HasWall;

        public bool IsFullBlock =>
            tile.BlockType == BlockType.Solid;

        /// <summary>
        /// Whether the tile/wall at this position ACTUALLY blocks light.
        /// </summary>
        public bool BlocksLight =>
            !tile.IsAir &&
            (tile.HasSolidTile || tile.HasSolidWall);
    }
}
