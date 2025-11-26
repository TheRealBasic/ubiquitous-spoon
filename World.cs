using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace NightclubSim
{
    public class World
    {
        public int Width { get; }
        public int Height { get; }
        private readonly Tile[,] _tiles;
        public Point Entrance { get; private set; }

        public World(int width, int height)
        {
            Width = width;
            Height = height;
            _tiles = new Tile[width, height];
            GenerateDefaultLayout();
        }

        private void GenerateDefaultLayout()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var type = (x == 0 || y == 0 || x == Width - 1 || y == Height - 1) ? TileType.Wall : TileType.Floor;
                    _tiles[x, y] = new Tile(x, y, type);
                }
            }
            Entrance = new Point(Width / 2, Height - 1);
            _tiles[Entrance.X, Entrance.Y].Type = TileType.Entrance;
            // Seed a few dance tiles
            SetTileType(3, 3, TileType.DanceFloor);
            SetTileType(4, 3, TileType.DanceFloor);
            SetTileType(3, 4, TileType.DanceFloor);
            SetTileType(4, 4, TileType.DanceFloor);
        }

        public Tile GetTile(int x, int y) => _tiles[x, y];

        public bool IsInside(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        public bool CanPlace(int x, int y)
        {
            if (!IsInside(x, y)) return false;
            var tile = _tiles[x, y];
            return tile.Type != TileType.Wall && tile.PlacedObject == null && tile.Type != TileType.Entrance;
        }

        public bool Place(int x, int y, Placeable placeable)
        {
            if (!CanPlace(x, y)) return false;
            var tile = _tiles[x, y];
            tile.PlacedObject = placeable;
            tile.Type = placeable.AppliedTileType;
            return true;
        }

        public bool Remove(int x, int y)
        {
            if (!IsInside(x, y)) return false;
            var tile = _tiles[x, y];
            if (tile.PlacedObject == null) return false;
            tile.PlacedObject = null;
            tile.Type = TileType.Floor;
            return true;
        }

        public IEnumerable<Tile> Tiles()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    yield return _tiles[x, y];
                }
            }
        }

        public void SetTileType(int x, int y, TileType type)
        {
            if (!IsInside(x, y)) return;
            _tiles[x, y].Type = type;
        }
    }
}
