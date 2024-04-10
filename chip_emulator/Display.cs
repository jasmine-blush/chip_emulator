using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections;
using System.Collections.Generic;

namespace chip_emulator
{
    internal static class Display
    {
        private static readonly BitArray _screen = new(Window.SCREEN_WIDTH * Window.SCREEN_HEIGHT);
        private static RenderTarget2D _displayTarget;

        private static Texture2D _pixelTexture;

        internal static void InitializeDisplay(GraphicsDevice device)
        {
            _displayTarget = new RenderTarget2D(device, Window.SCREEN_WIDTH, Window.SCREEN_HEIGHT);

            Color[] data = new Color[1];
            data[0] = Color.Green;
            _pixelTexture = new Texture2D(device, 1, 1);
            _pixelTexture.SetData(data);
        }

        private static void Render(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin();
            spriteBatch.GraphicsDevice.SetRenderTarget(_displayTarget);

            for(int i = 0; i < _screen.Length; i++)
            {
                if(_screen[i] == true)
                {
                    int x = i % Window.SCREEN_WIDTH;
                    int y = i / Window.SCREEN_WIDTH;
                    spriteBatch.Draw(_pixelTexture, new Vector2(x, y), Color.White);
                }
            }

            spriteBatch.End();
            spriteBatch.GraphicsDevice.SetRenderTarget(null);
        }

        internal static Texture2D GetDisplay(SpriteBatch spriteBatch)
        {
            Render(spriteBatch);
            return _displayTarget;
        }

        internal static void Clear()
        {
            _screen.SetAll(false);
        }

        internal static bool DisplaySprite(List<byte> spriteData, int x, int y)
        {
            bool collision = false;
            x %= Window.SCREEN_WIDTH;
            y %= Window.SCREEN_HEIGHT;

            int startingPosition = x + (y * Window.SCREEN_WIDTH);
            for(int line = 0; line < spriteData.Count; line++)
            {
                byte data = spriteData[line];
                int currPosition = startingPosition + (line * Window.SCREEN_WIDTH);

                //Go through all 8 bits since spriteData is a byte list
                for(int bit = 0; bit <= 7; bit++)
                {
                    //If sprite goes out of screen on right side, bound back to left side
                    if(currPosition + bit >= (y + line + 1) * Window.SCREEN_WIDTH)
                    {
                        currPosition -= Window.SCREEN_WIDTH;
                    }

                    bool spriteValue = GetBit(data, bit) != 0;
                    bool newValue = _screen[currPosition + bit] ^ spriteValue;
                    _screen[currPosition + bit] = newValue;

                    if(!newValue && spriteValue)
                    {
                        collision = true;
                    }
                }
            }
            return collision;
        }

        private static int GetBit(byte data, int index)
        {
            return (data >> (7 - index)) & 0x01;
        }
    }
}
