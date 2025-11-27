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
        /// <summary>
        /// Static offset for placing the map on screen. This keeps world math symmetrical
        /// between screen-to-grid and grid-to-screen operations so highlights align with the cursor.
        /// </summary>
        public Vector2 Origin { get; set; } = Vector2.Zero;

        public Vector2 ToScreen(int gridX, int gridY)
        {
            float x = (gridX - gridY) * (TileWidth / 2f);
            float y = (gridX + gridY) * (TileHeight / 2f);
            return new Vector2(x, y) * Zoom + Camera + Origin;
        }

        public Point ToGrid(Vector2 screen)
        {
            // Inverse of the isometric transform. Assumes camera offset and zoom.
            var adjusted = (screen - Camera - Origin) / Zoom;
            float gridX = (adjusted.X / (TileWidth / 2f) + adjusted.Y / (TileHeight / 2f)) / 2f;
            float gridY = (adjusted.Y / (TileHeight / 2f) - adjusted.X / (TileWidth / 2f)) / 2f;
            // Round to the nearest tile center rather than flooring so cursor highlights match
            // the drawn tile diamond even when zoomed.
            return new Point((int)System.Math.Round(gridX), (int)System.Math.Round(gridY));
        }

        public void DrawTile(SpriteBatch spriteBatch, Texture2D tileTexture, Tile tile)
        {
            var pos = ToScreen(tile.GridPosition.X, tile.GridPosition.Y);
            var color = tile.Type switch
            {
                TileType.Wall => new Color(40, 40, 64),
                TileType.Entrance => new Color(255, 135, 80),
                TileType.DanceFloor => new Color(195, 140, 255),
                TileType.Bar => new Color(170, 110, 70),
                TileType.Table => new Color(170, 60, 80),
                TileType.Decor => new Color(70, 215, 140),
                _ => new Color(125, 125, 140)
            };
            spriteBatch.Draw(tileTexture, pos, null, color, 0f, new Vector2(tileTexture.Width / 2f, tileTexture.Height / 2f), Zoom, SpriteEffects.None, 0f);

            // Subtle inset highlight to add depth.
            var accent = Color.Lerp(color, Color.White, 0.35f);
            spriteBatch.Draw(tileTexture, pos + new Vector2(0, -2f) * Zoom, null, accent * 0.35f, 0f, new Vector2(tileTexture.Width / 2f, tileTexture.Height / 2f), Zoom * 0.82f, SpriteEffects.None, 0f);
        }
    }
}
