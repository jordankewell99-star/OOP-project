//game1 final
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace UMLGameExample
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteFont _font;
        private Texture2D _pixel;

        private GameManager gm;
        private Player player;
        private UIManager ui;
        private SoundManager sound;

        private SoundEffect jumpFX;
        private SoundEffect hitFX;
        private SoundEffect collectFX;
        private SoundEffect useFX;
        private Song bgmSong;

        private int menuIndex = 0;
        private int pauseIndex = 0;
        private readonly string[] menuOptions = { "Start Game", "Options", "Exit" };
        private readonly string[] pauseOptions = { "Resume", "Main Menu", "Exit" };

        private float groundY = 400f;
        private float gravity = 600f;
        private float jumpVelocity = -350f;

        private float obstTimer = 0f;
        private float obstInterval = 2f;

        private float enemyTimer = 0f;
        private float enemyInterval = 5f;

        private float chestTimer = 0f;
        private float chestInterval = 6f;

        private bool nextPowerIsRegen = true;

        private float timeSurvived = 0f;
        private float timeScoreTimer = 0f;
        private int highScore = 0;

        private float lastDt = 0f;
        private KeyboardState prev;
        private Random rng = new Random();

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 600;
        }

        protected override void Initialize()
        {
            gm = new GameManager();
            player = new Player();
            ui = new UIManager();
            sound = new SoundManager();
            player.Position = new Vector2(100, groundY - 50);
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _font = Content.Load<SpriteFont>("DefaultFont");

            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            jumpFX = Content.Load<SoundEffect>("jump");
            hitFX = Content.Load<SoundEffect>("hit");
            collectFX = Content.Load<SoundEffect>("collect");
            useFX = Content.Load<SoundEffect>("use");
            bgmSong = Content.Load<Song>("bgm");

            MediaPlayer.IsRepeating = true;
            MediaPlayer.Volume = sound.MusicVolume;
            MediaPlayer.Play(bgmSong);
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState k = Keyboard.GetState();
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            lastDt = dt;

            if (k.IsKeyDown(Keys.Escape) && prev.IsKeyUp(Keys.Escape))
                HandleESC();

            switch (gm.CurrentState)
            {
                case GameState.MENU:
                    MenuUpdate(k);
                    break;
                case GameState.PAUSED:
                    PauseUpdate(k);
                    break;
                case GameState.PLAYING:
                    GamePlayingUpdate(dt, k);
                    break;
                case GameState.GAME_OVER:
                    GameOverUpdate(k);
                    break;
                case GameState.LEVEL_COMPLETE:
                    LevelCompleteUpdate(k);
                    break;
            }

            prev = k;
            base.Update(gameTime);
        }

        private void HandleESC()
        {
            if (gm.CurrentState == GameState.PLAYING)
                gm.CurrentState = GameState.PAUSED;
            else if (gm.CurrentState == GameState.PAUSED)
                gm.CurrentState = GameState.PLAYING;
            else if (gm.CurrentState == GameState.MENU)
                Exit();
            else if (gm.CurrentState == GameState.GAME_OVER || gm.CurrentState == GameState.LEVEL_COMPLETE)
                gm.CurrentState = GameState.MENU;
        }

        private void MenuUpdate(KeyboardState k)
        {
            if (ui.CurrentMenu == MenuState.MAIN_MENU)
            {
                if (k.IsKeyDown(Keys.Up) && prev.IsKeyUp(Keys.Up))
                    menuIndex = Math.Max(0, menuIndex - 1);

                if (k.IsKeyDown(Keys.Down) && prev.IsKeyUp(Keys.Down))
                    menuIndex = Math.Min(menuIndex + 1, menuOptions.Length - 1);

                if (k.IsKeyDown(Keys.Enter) && prev.IsKeyUp(Keys.Enter))
                {
                    if (menuIndex == 0)
                    {
                        gm.StartGame();
                        player = new Player();
                        player.Position = new Vector2(100, groundY - 50);
                        ResetTimers();
                        SpawnInitialChest();
                    }
                    else if (menuIndex == 1)
                        ui.ShowOptionMenu();
                    else if (menuIndex == 2)
                        Exit();
                }
            }
            else if (ui.CurrentMenu == MenuState.OPTIONS_MENU)
            {
                OptionsUpdate(k);
            }
        }

        private void OptionsUpdate(KeyboardState k)
        {
            if (k.IsKeyDown(Keys.A) && prev.IsKeyUp(Keys.A))
                ui.OptionsMenu.HandleTabNavigation(-1);

            if (k.IsKeyDown(Keys.D) && prev.IsKeyUp(Keys.D))
                ui.OptionsMenu.HandleTabNavigation(1);

            var current = ui.OptionsMenu.CurrentTab;

            if (current == ui.OptionsMenu.SoundTab)
            {
                var soundTab = ui.OptionsMenu.SoundTab;

                if (k.IsKeyDown(Keys.Left) && prev.IsKeyUp(Keys.Left))
                {
                    float newMaster = soundTab.MasterVolume - 0.1f;
                    float newMusic = soundTab.MusicVolume - 0.1f;
                    soundTab.OnVolumeChanged(newMaster, newMusic);
                    sound.SetVolumes(soundTab.MasterVolume, soundTab.MusicVolume);
                    MediaPlayer.Volume = sound.MusicVolume;
                }

                if (k.IsKeyDown(Keys.Right) && prev.IsKeyUp(Keys.Right))
                {
                    float newMaster = soundTab.MasterVolume + 0.1f;
                    float newMusic = soundTab.MusicVolume + 0.1f;
                    soundTab.OnVolumeChanged(newMaster, newMusic);
                    sound.SetVolumes(soundTab.MasterVolume, soundTab.MusicVolume);
                    MediaPlayer.Volume = sound.MusicVolume;
                }
            }
            else if (current == ui.OptionsMenu.UITab)
            {
                var uiTab = ui.OptionsMenu.UITab;

                if (k.IsKeyDown(Keys.Up) && prev.IsKeyUp(Keys.Up))
                {
                    bool newShow = !uiTab.ShowFPS;
                    uiTab.ApplyUISettings(newShow, uiTab.UIScale);
                }

                if (k.IsKeyDown(Keys.Left) && prev.IsKeyUp(Keys.Left))
                {
                    float newScale = uiTab.UIScale - 0.1f;
                    uiTab.ApplyUISettings(uiTab.ShowFPS, newScale);
                }

                if (k.IsKeyDown(Keys.Right) && prev.IsKeyUp(Keys.Right))
                {
                    float newScale = uiTab.UIScale + 0.1f;
                    uiTab.ApplyUISettings(uiTab.ShowFPS, newScale);
                }
            }

            if (k.IsKeyDown(Keys.Back) && prev.IsKeyUp(Keys.Back))
                ui.ShowMainMenu();
        }

        private void SpawnInitialChest()
        {
            SpawnChestSource();
        }

        private void PauseUpdate(KeyboardState k)
        {
            if (k.IsKeyDown(Keys.Up) && prev.IsKeyUp(Keys.Up))
                pauseIndex = Math.Max(0, pauseIndex - 1);

            if (k.IsKeyDown(Keys.Down) && prev.IsKeyUp(Keys.Down))
                pauseIndex = Math.Min(pauseIndex + 1, pauseOptions.Length - 1);

            if (k.IsKeyDown(Keys.Enter) && prev.IsKeyUp(Keys.Enter))
            {
                if (pauseIndex == 0)
                    gm.CurrentState = GameState.PLAYING;
                else if (pauseIndex == 1)
                    gm.CurrentState = GameState.MENU;
                else if (pauseIndex == 2)
                    Exit();
            }
        }

        private void GamePlayingUpdate(float dt, KeyboardState k)
        {
            timeSurvived += dt;
            timeScoreTimer += dt;

            if (timeScoreTimer >= 1f)
            {
                gm.AddScore(1);
                timeScoreTimer -= 1f;
            }

            gm.UpdateGame(dt);
            player.Update(dt);

            PlayerInput(k);
            ApplyGravity(dt);
            UpdateObjects(dt);
            SpawnObjects(dt);
            Collisions();

            if (player.Lives <= 0)
                gm.EndGame();
        }

        private void PlayerInput(KeyboardState k)
        {
            if (k.IsKeyDown(Keys.Space) && prev.IsKeyUp(Keys.Space)
                && player.Position.Y >= groundY - 50)
            {
                player.VelocityY = jumpVelocity;
                player.Jump();
                jumpFX.Play(sound.MasterVolume, 0f, 0f);
            }

            if (k.IsKeyDown(Keys.Down))
                player.SetSliding(true);
            else
                player.SetSliding(false);

            if (k.IsKeyDown(Keys.LeftShift))
                player.Run();
            else
                player.Walk();

            if (k.IsKeyDown(Keys.U) && prev.IsKeyUp(Keys.U))
            {
                if (player.CollectedPowerUps.Count > 0)
                {
                    var type = player.CollectedPowerUps[0];
                    player.UsePowerUp(type);
                    useFX.Play(sound.MasterVolume, 0f, 0f);
                }
            }

            if (k.IsKeyDown(Keys.F) && prev.IsKeyUp(Keys.F))
            {
                gm.bladeManager.SpawnBlade(new Vector2(player.Position.X + 25, player.Position.Y + 20));
            }
        }

        private void ApplyGravity(float dt)
        {
            player.VelocityY += gravity * dt;
            player.Position = new Vector2(player.Position.X, player.Position.Y + player.VelocityY * dt);

            if (player.Position.Y > groundY - 50)
            {
                player.Position = new Vector2(player.Position.X, groundY - 50);
                player.VelocityY = 0f;
                player.OnGrounded();
            }
        }

        private void UpdateObjects(float dt)
        {
            float speedFactor = gm.GameSpeed * player.RunSpeed;

            foreach (var o in gm.obstacles.ToArray())
            {
                o.Move(dt * speedFactor);
                if (o.Position.X < -50)
                {
                    gm.AddScore(10);
                    gm.obstacles.Remove(o);
                }
            }

            foreach (var e in gm.enemies.ToArray())
            {
                e.Move(dt * speedFactor);
                if (e.Position.X < -50)
                {
                    gm.AddScore(20);
                    gm.enemies.Remove(e);
                }
            }

            foreach (var s in gm.powerUpSources.ToArray())
            {
                if (!s.IsActive) continue;

                s.Update(dt);
                s.Position += new Vector2(-150 * dt * speedFactor, 0);

                if (s.Position.X < -50)
                    gm.powerUpSources.Remove(s);
            }

            gm.bladeManager.Update(dt * speedFactor);
            gm.bladeManager.CheckEnemyHit(gm.enemies);
        }

        private void SpawnObjects(float dt)
        {
            obstTimer += dt;
            enemyTimer += dt;
            chestTimer += dt;

            float obstTarget = obstInterval / gm.Difficulty;
            float enemyTarget = enemyInterval / gm.Difficulty;
            float chestTarget = chestInterval / gm.Difficulty;

            if (obstTimer >= obstTarget)
            {
                SpawnObstacle();
                obstTimer = 0f;
            }

            if (enemyTimer >= enemyTarget)
            {
                SpawnEnemy();
                enemyTimer = 0f;
            }

            if (chestTimer >= chestTarget)
            {
                SpawnChestSource();
                chestTimer = 0f;
            }
        }

        private void SpawnObstacle()
        {
            gm.obstacles.Add(new Obstacle(
                ObstacleType.STATIC_BLOCK,
                new Vector2(800, groundY - 30),
                260f));
        }

        private void SpawnEnemy()
        {
            gm.enemies.Add(new Enemy(
                EnemyType.GROUND_ENEMY,
                new Vector2(800, groundY - 40),
                230f));
        }

        private void SpawnChestSource()
        {
            PowerUpType typeToGive = nextPowerIsRegen ? PowerUpType.TEMP_HEALTH : PowerUpType.SHIELD;

            var chest = new PowerUpSource(
                PowerUpSourceType.CHEST,
                new Vector2(800, groundY - 45));

            chest.NextPowerType = typeToGive;
            gm.powerUpSources.Add(chest);
            nextPowerIsRegen = !nextPowerIsRegen;
        }

        private void Collisions()
        {
            Rectangle pr = new Rectangle((int)player.Position.X, (int)player.Position.Y, 35, 55);

            foreach (var o in gm.obstacles.ToArray())
            {
                Rectangle r = new Rectangle((int)o.Position.X, (int)o.Position.Y, 40, 40);
                if (pr.Intersects(r))
                {
                    player.TakeDamage(10);
                    hitFX.Play(sound.MasterVolume, 0f, 0f);
                    gm.obstacles.Remove(o);
                    break;
                }
            }

            foreach (var e in gm.enemies.ToArray())
            {
                Rectangle r = new Rectangle((int)e.Position.X, (int)e.Position.Y, 35, 35);
                if (pr.Intersects(r))
                {
                    e.Attack(player);
                    hitFX.Play(sound.MasterVolume, 0f, 0f);
                    gm.enemies.Remove(e);
                    break;
                }
            }

            foreach (var s in gm.powerUpSources.ToArray())
            {
                Rectangle sr = new Rectangle((int)s.Position.X, (int)s.Position.Y, 35, 35);
                if (pr.Intersects(sr))
                {
                    player.AddPowerUp(s.NextPowerType);
                    collectFX.Play(sound.MasterVolume, 0f, 0f);
                    gm.AddScore(5);
                    gm.powerUpSources.Remove(s);
                    break;
                }
            }
        }

        private void GameOverUpdate(KeyboardState k)
        {
            if (gm.Score > highScore)
                highScore = gm.Score;

            if (k.IsKeyDown(Keys.Enter) && prev.IsKeyUp(Keys.Enter))
            {
                gm.StartGame();
                player = new Player();
                player.Position = new Vector2(100, groundY - 50);
                ResetTimers();
                SpawnInitialChest();
            }

            if (k.IsKeyDown(Keys.M) && prev.IsKeyUp(Keys.M))
                gm.CurrentState = GameState.MENU;
        }

        private void LevelCompleteUpdate(KeyboardState k)
        {
            if (gm.Score > highScore)
                highScore = gm.Score;

            if (k.IsKeyDown(Keys.Enter) && prev.IsKeyUp(Keys.Enter))
            {
                gm.StartGame();
                player = new Player();
                player.Position = new Vector2(100, groundY - 50);
                ResetTimers();
                SpawnInitialChest();
            }

            if (k.IsKeyDown(Keys.M) && prev.IsKeyUp(Keys.M))
                gm.CurrentState = GameState.MENU;
        }

        private void ResetTimers()
        {
            timeSurvived = 0f;
            timeScoreTimer = 0f;
            obstTimer = 0f;
            enemyTimer = 0f;
            chestTimer = 0f;
            nextPowerIsRegen = true;
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.DarkGray);
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);

            if (gm.CurrentState == GameState.MENU)
                DrawMenu();
            else if (gm.CurrentState == GameState.PLAYING)
            {
                DrawWorld();
                DrawHUD();
            }
            else if (gm.CurrentState == GameState.PAUSED)
            {
                DrawWorld();
                DrawPauseMenu();
            }
            else if (gm.CurrentState == GameState.GAME_OVER)
                DrawGameOver();
            else if (gm.CurrentState == GameState.LEVEL_COMPLETE)
                DrawLevelComplete();

            _spriteBatch.End();
            base.Draw(gameTime);
        }

        private void DrawMenu()
        {
            DrawRect(new Rectangle(0, 0, 800, 600), Color.Black * 0.5f);

            if (ui.CurrentMenu == MenuState.MAIN_MENU)
            {
                _spriteBatch.DrawString(_font, "PLATFORMER GAME", new Vector2(260, 120), Color.White);

                for (int i = 0; i < menuOptions.Length; i++)
                {
                    Color c = (i == menuIndex) ? Color.Yellow : Color.White;

                    DrawRect(new Rectangle(280, 200 + i * 60, 260, 40),
                        (i == menuIndex) ? Color.DarkGray : Color.Gray * 0.5f);

                    _spriteBatch.DrawString(_font, menuOptions[i],
                        new Vector2(310, 210 + i * 60), c);
                }
            }
            else if (ui.CurrentMenu == MenuState.OPTIONS_MENU)
            {
                var soundTab = ui.OptionsMenu.SoundTab;
                var uiTab = ui.OptionsMenu.UITab;
                var current = ui.OptionsMenu.CurrentTab;

                _spriteBatch.DrawString(_font, "OPTIONS", new Vector2(330, 100), Color.White);

                _spriteBatch.DrawString(_font, current == soundTab ? "> Sound <" : "Sound",
                    new Vector2(220, 150), current == soundTab ? Color.Yellow : Color.White);

                _spriteBatch.DrawString(_font, current == uiTab ? "> UI <" : "UI",
                    new Vector2(420, 150), current == uiTab ? Color.Yellow : Color.White);

                if (current == soundTab)
                {
                    _spriteBatch.DrawString(_font, $"Master Volume: {(int)(soundTab.MasterVolume * 100)}%", new Vector2(220, 220), Color.White);
                    _spriteBatch.DrawString(_font, $"Music Volume:  {(int)(soundTab.MusicVolume * 100)}%", new Vector2(220, 260), Color.White);
                    _spriteBatch.DrawString(_font, "Left/Right: change volume", new Vector2(220, 320), Color.Gray);
                }
                else if (current == uiTab)
                {
                    _spriteBatch.DrawString(_font, $"Show FPS: {(uiTab.ShowFPS ? "ON" : "OFF")}", new Vector2(220, 220), Color.White);
                    _spriteBatch.DrawString(_font, $"UI Scale:  {uiTab.UIScale:0.0}x", new Vector2(220, 260), Color.White);
                    _spriteBatch.DrawString(_font, "Up: toggle FPS  Left/Right: scale", new Vector2(220, 320), Color.Gray);
                }

                _spriteBatch.DrawString(_font, "A / D: switch tab   Backspace: Main Menu", new Vector2(160, 360), Color.Gray);
            }
        }

        private void DrawPauseMenu()
        {
            DrawRect(new Rectangle(0, 0, 800, 600), Color.Black * 0.5f);

            _spriteBatch.DrawString(_font, "GAME PAUSED", new Vector2(300, 120), Color.White);

            for (int i = 0; i < pauseOptions.Length; i++)
            {
                Color c = (i == pauseIndex) ? Color.Yellow : Color.White;

                DrawRect(new Rectangle(280, 200 + i * 60, 260, 40),
                    (i == pauseIndex) ? Color.DarkGray : Color.Gray * 0.5f);

                _spriteBatch.DrawString(_font, pauseOptions[i],
                    new Vector2(310, 210 + i * 60), c);
            }
        }

        private void DrawWorld()
        {
            DrawRect(new Rectangle(0, (int)groundY, 800, 200), Color.Green);

            int playerHeight = player.IsSliding ? 35 : 55;
            Color bodyColor = player.IsShielded ? Color.Cyan : Color.DarkRed;

            DrawRect(new Rectangle((int)player.Position.X, (int)player.Position.Y, 35, playerHeight), bodyColor);
            DrawRect(new Rectangle((int)player.Position.X + 5, (int)player.Position.Y + 5, 25, Math.Max(10, playerHeight - 10)), Color.OrangeRed);

            // ✅ SHIELD VISUAL EFFECT
            if (player.IsShielded)
            {
                DrawRect(new Rectangle(
                    (int)player.Position.X - 5,
                    (int)player.Position.Y - 5,
                    45,
                    player.IsSliding ? 45 : 65),
                    Color.Cyan * 0.4f);
            }

            foreach (var o in gm.obstacles)
                DrawRect(new Rectangle((int)o.Position.X, (int)o.Position.Y, 40, 40), Color.SaddleBrown);

            foreach (var e in gm.enemies)
                DrawRect(new Rectangle((int)e.Position.X, (int)e.Position.Y, 35, 35), Color.Purple);

            foreach (var s in gm.powerUpSources)
            {
                Color c = s.NextPowerType == PowerUpType.TEMP_HEALTH
                    ? Color.Red
                    : Color.CornflowerBlue;

                DrawRect(new Rectangle((int)s.Position.X, (int)s.Position.Y, 35, 35), c);
            }

            foreach (var b in gm.bladeManager.blades)
                DrawRect(new Rectangle((int)b.Position.X, (int)b.Position.Y, 20, 5), Color.Black);
        }

        private void DrawHUD()
        {
            float uiScale = ui.OptionsMenu.UITab.UIScale;

            DrawTextScaled($"Score: {gm.Score}", new Vector2(10, 10), Color.White, uiScale);
            DrawTextScaled($"High:  {highScore}", new Vector2(10, 30), Color.White, uiScale);
            DrawTextScaled($"Lives: {player.Lives}", new Vector2(10, 50), Color.White, uiScale);
            DrawTextScaled($"Health:{player.Health}", new Vector2(10, 70), Color.White, uiScale);
            DrawTextScaled($"Time:  {(int)timeSurvived}s", new Vector2(10, 90), Color.White, uiScale);

            int shieldCount = player.CountPowerUp(PowerUpType.SHIELD);
            int regenCount = player.CountPowerUp(PowerUpType.TEMP_HEALTH);

            // ✅ SHOW ACTIVE STATUS
            string shieldStatus = player.IsShielded ? "ACTIVE" : shieldCount.ToString();
            DrawTextScaled($"Shield: {shieldStatus}", new Vector2(10, 110), Color.Cyan, uiScale);

            DrawTextScaled($"Regen : {regenCount}", new Vector2(10, 130), Color.Red, uiScale);

            if (player.CollectedPowerUps.Count > 0)
            {
                DrawRect(new Rectangle(10, 150, 190, 30), Color.Blue * 0.6f);
                DrawTextScaled("Use Power-Up (U)", new Vector2(15, 155), Color.White, uiScale);
            }

            DrawTextScaled($"Level: {gm.CurrentLevel}", new Vector2(600, 40), Color.White, uiScale);
            DrawTextScaled($"Diff : {gm.Difficulty:0.0}", new Vector2(600, 60), Color.White, uiScale);

            if (ui.OptionsMenu.UITab.ShowFPS && lastDt > 0f)
            {
                float fps = 1f / lastDt;
                DrawTextScaled($"FPS: {(int)fps}", new Vector2(700, 10), Color.Yellow, uiScale);
            }
        }

        private void DrawGameOver()
        {
            DrawRect(new Rectangle(0, 0, 800, 600), Color.Black * 0.5f);

            _spriteBatch.DrawString(_font, "GAME OVER", new Vector2(320, 80), Color.Red);
            _spriteBatch.DrawString(_font, $"Score:      {gm.Score}", new Vector2(280, 140), Color.White);
            _spriteBatch.DrawString(_font, $"High Score: {highScore}", new Vector2(280, 170), Color.White);
            _spriteBatch.DrawString(_font, $"Time:       {(int)timeSurvived}s", new Vector2(280, 200), Color.White);

            _spriteBatch.DrawString(_font, "Press ENTER to Restart", new Vector2(260, 260), Color.White);
            _spriteBatch.DrawString(_font, "Press M for Menu", new Vector2(290, 300), Color.White);
        }

        private void DrawLevelComplete()
        {
            DrawRect(new Rectangle(0, 0, 800, 600), Color.Black * 0.5f);
            _spriteBatch.DrawString(_font, "LEVEL COMPLETE", new Vector2(270, 100), Color.Yellow);
            _spriteBatch.DrawString(_font, "Press ENTER to Continue", new Vector2(240, 250), Color.White);
        }

        private void DrawRect(Rectangle r, Color c)
        {
            _spriteBatch.Draw(_pixel, r, c);
        }

        private void DrawTextScaled(string text, Vector2 position, Color color, float scale)
        {
            _spriteBatch.DrawString(_font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}

