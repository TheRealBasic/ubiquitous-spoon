using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NightclubSim
{
    public static class TextureFactory
    {
        public static Texture2D CreateDiamond(GraphicsDevice device, int width, int height)
        {
            var texture = new Texture2D(device, width, height);
            var data = new Color[width * height];
            int hw = width / 2;
            int hh = height / 2;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int dx = System.Math.Abs(x - hw);
                    int dy = System.Math.Abs(y - hh);
                    bool inside = dx * hh + dy * hw <= hw * hh;
                    data[y * width + x] = inside ? Color.White : Color.Transparent;
                }
            }
            texture.SetData(data);
            return texture;
        }

        public static Texture2D CreateRectangle(GraphicsDevice device, int width, int height, Color color)
        {
            var texture = new Texture2D(device, width, height);
            var data = new Color[width * height];
            for (int i = 0; i < data.Length; i++) data[i] = color;
            texture.SetData(data);
            return texture;
        }
    }
}
