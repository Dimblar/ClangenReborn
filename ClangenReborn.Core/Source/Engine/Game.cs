using ClangenReborn.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using static ClangenReborn.Content;

namespace ClangenReborn
{
    public class ClangenNetGame : Game
    {
        private static GraphicsDeviceManager? GraphicsDeviceManager;
        public static Utility.FramesPerSecondCounter FPS { get; private set; } = new();

        public ClangenNetGame()
        {
            GraphicsDeviceManager = new GraphicsDeviceManager(this)
            {
                GraphicsProfile = GraphicsProfile.HiDef,
                PreferredBackBufferWidth = 800,
                PreferredBackBufferHeight = 700,
            };

            //GraphicsDeviceManager.IsFullScreen = true;
            //GraphicsDeviceManager.HardwareModeSwitch = false;

            // TODO Remove -> temporary to test other framerates
            //this.TargetElapsedTime = new TimeSpan(0, 0, 0, 0, 64);

            this.IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            if (GraphicsDeviceManager is null)
                throw new Exception();

            PrepareContext(GraphicsDeviceManager);
            base.LoadContent();
        }

        protected override void UnloadContent()
        {
            base.UnloadContent();
        }

        protected override void Update(GameTime GameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            ClangenReborn.Content.Update(GameTime);
            CurrentScene?.Update(GameTime); // FIX -> deal with delta time and ticks per second
            base.Update(GameTime);
        }

        protected override void Draw(GameTime GameTime)
        {
            FPS.Update(GameTime);
            ClangenReborn.Content.Draw();
            base.Draw(GameTime);
        }

        protected override void BeginRun()
        {
            SetScene<LoadingScreen>();
            base.BeginRun();
        }
    }
}