using Microsoft.Xna.Framework;

namespace NightclubSim
{
    public enum TileType
    {
        Empty,
        Floor,
        Wall,
        Entrance,
        DanceFloor,
        Bar,
        Table,
        Decor
    }

    public class Tile
    {
        public Point GridPosition { get; }
        public TileType Type { get; set; }
        public Placeable? PlacedObject { get; set; }

        public Tile(int x, int y, TileType type)
        {
            GridPosition = new Point(x, y);
            Type = type;
        }

        public bool Walkable => Type != TileType.Wall && PlacedObject?.BlocksMovement != true;
    }
}
