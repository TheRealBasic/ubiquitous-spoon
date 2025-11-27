using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace NightclubSim
{
    public class SaveData
    {
        public int Money { get; set; }
        public int Level { get; set; }
        public int Experience { get; set; }
        public float Rating { get; set; }
        public List<SavedTile> Tiles { get; set; } = new();
        public List<SavedStaff> Staff { get; set; } = new();
    }

    public record SavedTile(int X, int Y, TileType Type, PlaceableType Placeable);
    public record SavedStaff(StaffRole Role, int X, int Y);

    public static class SaveManager
    {
        private const string FileName = "club_save.json";

        public static void Save(World world, Economy economy, List<Staff> staff, float rating)
        {
            var data = new SaveData
            {
                Money = economy.Money,
                Level = economy.Level,
                Experience = economy.Experience,
                Rating = rating
            };
            foreach (var tile in world.Tiles())
            {
                data.Tiles.Add(new SavedTile(tile.GridPosition.X, tile.GridPosition.Y, tile.Type, tile.PlacedObject?.Type ?? PlaceableType.None));
            }
            foreach (var s in staff)
            {
                data.Staff.Add(new SavedStaff(s.Role, (int)s.GridPosition.X, (int)s.GridPosition.Y));
            }
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FileName, json);
        }

        public static bool TryLoad(World world, Economy economy, List<Staff> staff, out float rating)
        {
            rating = 1f;
            if (!File.Exists(FileName)) return false;
            try
            {
                var json = File.ReadAllText(FileName);
                var data = JsonSerializer.Deserialize<SaveData>(json);
                if (data == null) return false;
                economy.LoadState(data.Money, data.Level, data.Experience);
                rating = data.Rating <= 0 ? 1f : data.Rating;
                foreach (var tile in data.Tiles)
                {
                    var placeable = tile.Placeable != PlaceableType.None ? Placeable.Create(tile.Placeable) : null;
                    world.SetTileType(tile.X, tile.Y, tile.Type);
                    world.GetTile(tile.X, tile.Y).PlacedObject = placeable;
                }
                staff.Clear();
                foreach (var s in data.Staff)
                {
                    staff.Add(new Staff(s.Role, new Vector2(s.X, s.Y)));
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void Clear()
        {
            if (File.Exists(FileName)) File.Delete(FileName);
        }
    }
}
