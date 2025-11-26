using Microsoft.Xna.Framework;

namespace NightclubSim
{
    public enum StaffRole
    {
        Bartender,
        DJ,
        Bouncer
    }

    public class Staff
    {
        public StaffRole Role { get; }
        public Vector2 GridPosition { get; }
        public Color Color => Role switch
        {
            StaffRole.Bartender => Color.DarkOrange,
            StaffRole.DJ => Color.DeepSkyBlue,
            StaffRole.Bouncer => Color.DarkSlateBlue,
            _ => Color.White
        };

        public Staff(StaffRole role, Vector2 pos)
        {
            Role = role;
            GridPosition = pos;
        }
    }
}
