using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Threading;

namespace chip_emulator
{
    internal class Window : Game
    {
        internal const int SCREEN_WIDTH = 64;
        internal const int SCREEN_HEIGHT = 32;
        internal static readonly int DISPLAY_SCALE = 8; //Adjust as wanted to change displayed size of screen

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private readonly object _renderLock = new();

        private readonly Interpreter _interpreter;
        private Thread _interpreterThread;

        internal Window()
        {
            _graphics = new(this) {
                PreferredBackBufferWidth = SCREEN_WIDTH * DISPLAY_SCALE,
                PreferredBackBufferHeight = SCREEN_HEIGHT * DISPLAY_SCALE,
                PreferMultiSampling = false,
            };
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _interpreter = new Interpreter("BREAKOUT.ch8");
        }

        protected override void Initialize()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            Display.InitializeDisplay(_graphics.GraphicsDevice);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _interpreterThread = new Thread(() => _interpreter.Run());
            _interpreterThread.Start();
        }

        protected override void Update(GameTime gameTime)
        {
            if(GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            Texture2D renderTexture = Display.GetDisplay(_spriteBatch);
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(renderTexture, Vector2.Zero, null, Color.White, 0, Vector2.Zero, DISPLAY_SCALE, SpriteEffects.None, 0);
            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
