using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NightclubSim
{
    public static class TextureFactory
    {
        public static Texture2D CreateShadedDiamond(GraphicsDevice device, int width, int height)
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
                    if (!inside)
                    {
                        data[y * width + x] = Color.Transparent;
                        continue;
                    }

                    // Shaded center with subtle edge highlight for more visual depth.
                    float edgeFactor = (float)(dx + dy) / (hw + hh);
                    float inner = 1f - edgeFactor * 0.6f;
                    var shade = new Color(inner, inner, inner, 1f);
                    if (edgeFactor < 0.25f)
                    {
                        shade = Color.Lerp(shade, Color.White, 0.2f);
                    }
                    data[y * width + x] = shade;
                }
            }
            texture.SetData(data);
            return texture;
        }

        public static Texture2D CreateCharacter(GraphicsDevice device, int width, int height)
        {
            var texture = new Texture2D(device, width, height);
            var data = new Color[width * height];
            int headHeight = height / 4;
            int torsoHeight = height - headHeight;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool insideBody = x > 1 && x < width - 2 && y >= headHeight - 2;
                    bool insideHead = x > 2 && x < width - 3 && y < headHeight + 1;
                    if (!insideBody && !insideHead)
                    {
                        data[y * width + x] = Color.Transparent;
                        continue;
                    }

                    // Torso with gradient and belt highlight.
                    if (insideBody)
                    {
                        float t = (float)(y - headHeight) / torsoHeight;
                        var shade = Color.Lerp(new Color(180, 180, 200), new Color(110, 110, 140), t);
                        if (y == headHeight + torsoHeight / 2) shade = new Color(240, 240, 250);
                        data[y * width + x] = shade;
                    }

                    // Head with darker outline.
                    if (insideHead)
                    {
                        bool edge = x == 3 || x == width - 4 || y == headHeight;
                        var head = edge ? new Color(60, 60, 70) : new Color(200, 200, 220);
                        data[y * width + x] = head;
                    }
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
