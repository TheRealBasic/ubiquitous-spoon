using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NightclubSim
{
    /// <summary>
    /// Very small pixel font drawn from hard-coded glyphs so no external assets are required.
    /// Glyphs are 4x6 pixels represented as strings.
    /// </summary>
    public class PixelFont
    {
        private readonly Dictionary<char, bool[,]> _glyphs = new();
        private readonly Texture2D _pixel;
        public int GlyphWidth => 4;
        public int GlyphHeight => 6;

        public PixelFont(GraphicsDevice device)
        {
            _pixel = TextureFactory.CreateRectangle(device, 1, 1, Color.White);
            BuildGlyphs();
        }

        private void BuildGlyphs()
        {
            Add('0', "111110011001100110011111");
            Add('1', "011001100110011001100111");
            Add('2', "111100011111100110001111");
            Add('3', "111100011111000110011111");
            Add('4', "100110011111000100010001");
            Add('5', "111110001111000011111111");
            Add('6', "011010001111100110011111");
            Add('7', "111100011001000100010001");
            Add('8', "111110011111100110011111");
            Add('9', "111110011111000110010110");
            Add('A', "011010011111100110011001");
            Add('B', "111110011110100110011111");
            Add('C', "011010001000100010000110");
            Add('D', "111010011001100110011110");
            Add('E', "111110001110100010001111");
            Add('F', "111110001110100010001000");
            Add('G', "011010001011100110000110");
            Add('H', "100110011111100110011001");
            Add('I', "111001000100010001001110");
            Add('J', "001000100010010010010110");
            Add('K', "100110101100101010011001");
            Add('L', "100010001000100010001111");
            Add('M', "100111111111100110011001");
            Add('N', "100110111101110110011001");
            Add('O', "011010011001100110010110");
            Add('P', "111110011110100010001000");
            Add('Q', "011010011001100111010001");
            Add('R', "111110011110101010011001");
            Add('S', "011110000111000011110111");
            Add('T', "111001000100010001000100");
            Add('U', "100110011001100110011111");
            Add('V', "100110011001100101010010");
            Add('W', "100110011001111111111111");
            Add('X', "100101010010001001011001");
            Add('Y', "100110011111000100010001");
            Add('Z', "111100010010010010001111");
            Add(':', "000001000000010000000100");
            Add('$', "011111010111101011110110");
            Add(' ', "000000000000000000000000");
            Add('-', "000000000111000000000000");
            Add('/', "000100010010010010001000");
        }

        private void Add(char c, string pattern)
        {
            var glyph = new bool[GlyphWidth, GlyphHeight];
            for (int y = 0; y < GlyphHeight; y++)
            {
                for (int x = 0; x < GlyphWidth; x++)
                {
                    int idx = y * GlyphWidth + x;
                    glyph[x, y] = pattern[idx] == '1';
                }
            }
            _glyphs[c] = glyph;
        }

        public void DrawString(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float scale = 1f)
        {
            var cursor = position;
            foreach (char c in text)
            {
                if (c == '\n')
                {
                    cursor.X = position.X;
                    cursor.Y += GlyphHeight * scale + 2;
                    continue;
                }
                if (_glyphs.TryGetValue(char.ToUpperInvariant(c), out var glyph))
                {
                    for (int y = 0; y < GlyphHeight; y++)
                    {
                        for (int x = 0; x < GlyphWidth; x++)
                        {
                            if (glyph[x, y])
                            {
                                spriteBatch.Draw(_pixel, cursor + new Vector2(x * scale, y * scale), null, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                            }
                        }
                    }
                }
                cursor.X += GlyphWidth * scale + 1;
            }
        }
    }
}
