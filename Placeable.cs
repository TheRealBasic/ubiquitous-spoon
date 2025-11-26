using Microsoft.Xna.Framework;

namespace NightclubSim
{
    public enum PlaceableType
    {
        None,
        BarCounter,
        Booth,
        Speaker,
        Light,
        DanceTile,
        DecorPlant
    }

    public class Placeable
    {
        public PlaceableType Type { get; }
        public string Name { get; }
        public int Cost { get; }
        public int LevelRequirement { get; }
        public Color Color { get; }
        public bool BlocksMovement { get; }
        public TileType AppliedTileType { get; }

        public Placeable(PlaceableType type, string name, int cost, int levelRequirement, Color color, bool blocksMovement, TileType appliedTileType)
        {
            Type = type;
            Name = name;
            Cost = cost;
            LevelRequirement = levelRequirement;
            Color = color;
            BlocksMovement = blocksMovement;
            AppliedTileType = appliedTileType;
        }

        public static Placeable Create(PlaceableType type)
        {
            return type switch
            {
                PlaceableType.BarCounter => new Placeable(type, "Bar Counter", 150, 1, Color.SaddleBrown, true, TileType.Bar),
                PlaceableType.Booth => new Placeable(type, "Booth", 120, 1, Color.Maroon, false, TileType.Table),
                PlaceableType.Speaker => new Placeable(type, "Speaker", 200, 2, Color.DarkSlateGray, false, TileType.Decor),
                PlaceableType.Light => new Placeable(type, "Light Rig", 160, 2, Color.Gold, false, TileType.Decor),
                PlaceableType.DanceTile => new Placeable(type, "Dance Floor", 100, 1, Color.MediumPurple, false, TileType.DanceFloor),
                PlaceableType.DecorPlant => new Placeable(type, "Neon Plant", 80, 1, Color.LimeGreen, false, TileType.Decor),
                _ => new Placeable(PlaceableType.None, "None", 0, 0, Color.Transparent, false, TileType.Empty)
            };
        }
    }
}
