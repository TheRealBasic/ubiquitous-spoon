using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NightclubSim
{
    /// <summary>
    /// Helper responsible for converting grid coordinates to isometric screen coordinates and drawing tiles/entities.
    /// </summary>
    public class IsoRenderer
    {
        public int TileWidth { get; } = 64;
        public int TileHeight { get; } = 32;
        public Vector2 Camera { get; set; } = Vector2.Zero;
        public float Zoom { get; set; } = 1f;

        public Vector2 ToScreen(int gridX, int gridY)
        {
            float x = (gridX - gridY) * (TileWidth / 2f);
            float y = (gridX + gridY) * (TileHeight / 2f);
            return new Vector2(x, y) * Zoom + Camera;
        }

        public Point ToGrid(Vector2 screen)
        {
            // Inverse of the isometric transform. Assumes camera offset and zoom.
            var adjusted = (screen - Camera) / Zoom;
            float gridX = (adjusted.X / (TileWidth / 2f) + adjusted.Y / (TileHeight / 2f)) / 2f;
            float gridY = (adjusted.Y / (TileHeight / 2f) - adjusted.X / (TileWidth / 2f)) / 2f;
            return new Point((int)System.Math.Floor(gridX), (int)System.Math.Floor(gridY));
        }

        public void DrawTile(SpriteBatch spriteBatch, Texture2D tileTexture, Tile tile)
        {
            var pos = ToScreen(tile.GridPosition.X, tile.GridPosition.Y);
            var color = tile.Type switch
            {
                TileType.Wall => new Color(35, 35, 50),
                TileType.Entrance => new Color(255, 120, 60),
                TileType.DanceFloor => new Color(175, 120, 255),
                TileType.Bar => new Color(140, 90, 60),
                TileType.Table => new Color(140, 40, 60),
                TileType.Decor => new Color(60, 200, 120),
                _ => new Color(110, 110, 120)
            };
            spriteBatch.Draw(tileTexture, pos, null, color, 0f, new Vector2(tileTexture.Width / 2f, tileTexture.Height / 2f), Zoom, SpriteEffects.None, 0f);
        }
    }
}
