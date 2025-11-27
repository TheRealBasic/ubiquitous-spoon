using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NightclubSim
{
    public class FloatingText
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public string Text;
        public float Lifetime;
        public Color Color;
        public float Scale;

        public FloatingText(string text, Vector2 position, Color color, float lifetime = 1.5f, float scale = 1f)
        {
            Text = text;
            Position = position;
            Velocity = new Vector2(0, -15f);
            Color = color;
            Lifetime = lifetime;
            Scale = scale;
        }

        public bool Update(float dt)
        {
            Position += Velocity * dt;
            Lifetime -= dt;
            return Lifetime <= 0f;
        }

        public float Opacity => MathHelper.Clamp(Lifetime, 0f, 1f);
    }
}
