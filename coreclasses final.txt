//coreclasses final

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace UMLGameExample
{
    public enum GameState { MENU, PLAYING, GAME_OVER, PAUSED, LEVEL_COMPLETE }
    public enum MenuState { MAIN_MENU, OPTIONS_MENU }
    public enum ObstacleType { STATIC_BLOCK, MOVING_BARRIER, GAP, SPIKE }
    public enum EnemyType { GROUND_ENEMY, FLYING_ENEMY, PATROLLING_ENEMY }
    public enum PowerUpType { SHIELD, TEMP_HEALTH }
    public enum LevelType { TUTORIAL, MID_LEVEL, BOSS_LEVEL }
    public enum PowerUpSourceType { CHEST }

    public class GameManager
    {
        private int score;
        private float gameSpeed;
        private float difficulty;
        private GameState currentState;
        private LevelType currentLevel;

        public List<Obstacle> obstacles { get; private set; }
        public List<Enemy> enemies { get; private set; }
        public List<PowerUpSource> powerUpSources { get; private set; }
        public BladeManager bladeManager { get; private set; }

        public GameManager()
        {
            score = 0;
            gameSpeed = 1f;
            difficulty = 1f;
            currentState = GameState.MENU;
            currentLevel = LevelType.TUTORIAL;
            obstacles = new List<Obstacle>();
            enemies = new List<Enemy>();
            powerUpSources = new List<PowerUpSource>();
            bladeManager = new BladeManager();
        }

        public void StartGame()
        {
            score = 0;
            gameSpeed = 1f;
            difficulty = 1f;
            currentState = GameState.PLAYING;
            currentLevel = LevelType.TUTORIAL;
            obstacles.Clear();
            enemies.Clear();
            powerUpSources.Clear();
            bladeManager.blades.Clear();
        }

        public void NextLevel()
        {
            if (currentLevel == LevelType.TUTORIAL)
                currentLevel = LevelType.MID_LEVEL;
            else if (currentLevel == LevelType.MID_LEVEL)
                currentLevel = LevelType.BOSS_LEVEL;
            else
                currentState = GameState.LEVEL_COMPLETE;

            obstacles.Clear();
            enemies.Clear();
            powerUpSources.Clear();
            bladeManager.blades.Clear();
        }

        public void UpdateGame(float dt)
        {
            if (currentState != GameState.PLAYING)
                return;

            if (currentLevel == LevelType.TUTORIAL && score >= 100)
            {
                NextLevel();
                IncreaseDifficulty();
            }
            else if (currentLevel == LevelType.MID_LEVEL && score >= 300)
            {
                NextLevel();
                IncreaseDifficulty();
            }
        }

        public void IncreaseDifficulty()
        {
            difficulty += 0.1f;
            gameSpeed += 0.05f;
        }

        public void EndGame()
        {
            currentState = GameState.GAME_OVER;
        }

        public GameState CurrentState
        {
            get => currentState;
            set => currentState = value;
        }

        public LevelType CurrentLevel => currentLevel;
        public int Score => score;
        public float GameSpeed => gameSpeed;
        public float Difficulty => difficulty;

        public void AddScore(int amount)
        {
            score += amount;
        }
    }

    public class Player
    {
        private int health;
        private int lives;
        private bool isShielded;
        private Vector2 position;

        public float VelocityY { get; set; }
        public float RunSpeed { get; private set; }
        public List<PowerUpType> CollectedPowerUps { get; private set; }

        private bool isJumping;
        private bool isSliding;

        private bool hasRegen;
        private float regenTimer;
        private float regenRate;
        private float regenOverflow;

        public Player(int health = 100, int lives = 3)
        {
            this.health = health;
            this.lives = lives;
            isShielded = false;
            position = Vector2.Zero;
            VelocityY = 0f;
            RunSpeed = 1f;
            CollectedPowerUps = new List<PowerUpType>();
            isJumping = false;
            isSliding = false;
            hasRegen = false;
            regenTimer = 0f;
            regenRate = 0f;
            regenOverflow = 0f;
        }

        public void Update(float dt)
        {
            if (hasRegen)
            {
                if (regenTimer > 0f)
                {
                    regenTimer -= dt;

                    if (health < 100)
                    {
                        regenOverflow += regenRate * dt;

                        while (regenOverflow >= 1f && health < 100)
                        {
                            health++;
                            regenOverflow -= 1f;
                        }
                    }
                }

                if (regenTimer <= 0f)
                {
                    hasRegen = false;
                    regenTimer = 0f;
                    regenOverflow = 0f;
                }
            }
        }

        public void Jump()
        {
            isJumping = true;
        }

        public void OnGrounded()
        {
            isJumping = false;
        }

        public void SetSliding(bool sliding)
        {
            isSliding = sliding;
        }

        public void Run()
        {
            RunSpeed = 1.5f;
        }

        public void Walk()
        {
            RunSpeed = 1.0f;
        }

        public void TakeDamage(int amount)
        {
            if (isShielded)
            {
                isShielded = false;
                return;
            }

            health -= amount;

            if (health <= 0)
            {
                lives--;
                health = 100;
                hasRegen = false;
                regenTimer = 0f;
                regenOverflow = 0f;
                CollectedPowerUps.Clear();
            }
        }

        public void CollectPowerUp(PowerUpSource source)
        {
            CollectedPowerUps.Add(source.NextPowerType);
        }

        public void UsePowerUp(PowerUpType type)
        {
            if (!CollectedPowerUps.Contains(type))
                return;

            if (type == PowerUpType.SHIELD)
                isShielded = true;
            else if (type == PowerUpType.TEMP_HEALTH)
                StartRegen(5f, 3f);

            CollectedPowerUps.Remove(type);
        }

        public void AddPowerUp(PowerUpType type)
        {
            CollectedPowerUps.Add(type);
        }

        private void StartRegen(float duration, float ratePerSecond)
        {
            hasRegen = true;
            regenTimer = duration;
            regenRate = ratePerSecond;
            regenOverflow = 0f;
        }

        public Blade ThrowBlade()
        {
            return new Blade(new Vector2(position.X + 30, position.Y + 20));
        }

        public Vector2 Position
        {
            get => position;
            set => position = value;
        }

        public int Lives => lives;
        public int Health => health;
        public bool IsShielded => isShielded;
        public bool IsJumping => isJumping;
        public bool IsSliding => isSliding;

        public int CountPowerUp(PowerUpType type)
        {
            int count = 0;
            foreach (var p in CollectedPowerUps)
            {
                if (p == type)
                    count++;
            }
            return count;
        }
    }

    public class Obstacle
    {
        public ObstacleType Type { get; private set; }
        public float Speed { get; private set; }
        public Vector2 Position { get; set; }

        private bool moving;
        private float range;
        private float timer;
        private Vector2 start;

        public Obstacle(ObstacleType type, Vector2 position, float speed, bool moving = false, float range = 0f)
        {
            Type = type;
            Position = position;
            Speed = speed;
            this.moving = moving;
            this.range = range;
            timer = 0f;
            start = position;
        }

        public void Move(float dt)
        {
            if (moving)
            {
                timer += dt;
                float offset = (float)Math.Sin(timer * 2f) * range;
                Position = new Vector2(start.X - Speed * dt, start.Y + offset);
                start = new Vector2(start.X - Speed * dt, start.Y);
            }
            else
            {
                Position += new Vector2(-Speed * dt, 0);
            }
        }

        public void Spawn(Vector2 at)
        {
            Position = at;
            start = at;
        }
    }

    public class Enemy
    {
        public EnemyType Type { get; private set; }
        public float Speed { get; private set; }
        public Vector2 Position { get; set; }
        public int Health { get; private set; }

        private Vector2 start;
        private float t;
        private bool movingRight;

        public Enemy(EnemyType type, Vector2 position, float speed, int health = 30)
        {
            Type = type;
            Position = position;
            Speed = speed;
            Health = health;
            start = position;
            t = 0f;
            movingRight = true;
        }

        public void Move(float dt)
        {
            if (Type == EnemyType.PATROLLING_ENEMY)
            {
                t += dt;
                if (t > 2f)
                {
                    movingRight = !movingRight;
                    t = 0f;
                }

                float dir = movingRight ? 1f : -1f;
                Position += new Vector2(Speed * dir * dt, 0);
            }
            else if (Type == EnemyType.FLYING_ENEMY)
            {
                t += dt;
                float vertical = (float)Math.Sin(t * 3f) * 50f;
                Position = new Vector2(Position.X - Speed * dt, start.Y + vertical);
                start = new Vector2(start.X - Speed * dt, start.Y);
            }
            else
            {
                Position += new Vector2(-Speed * dt, 0);
            }
        }

        public void Attack(Player player)
        {
            player.TakeDamage(10);
        }

        public void TakeDamage(int amount)
        {
            Health -= amount;
        }

        public void Spawn(Vector2 at)
        {
            Position = at;
            start = at;
        }
    }

    public class PowerUpSource
    {
        public PowerUpSourceType Type { get; private set; }
        public Vector2 Position { get; set; }
        public bool IsActive { get; private set; }

        public PowerUpType NextPowerType { get; set; }

        private float baseY;

        public PowerUpSource(PowerUpSourceType type, Vector2 position)
        {
            Type = type;
            Position = position;
            IsActive = true;
            baseY = position.Y;
            NextPowerType = PowerUpType.SHIELD;
        }

        public void Update(float dt)
        {
            Position = new Vector2(Position.X, baseY);
        }

        public void Deactivate()
        {
            IsActive = false;
        }
    }

    public class Blade
    {
        public Vector2 Position { get; set; }
        public bool Active { get; set; }
        private float speed;

        public Blade(Vector2 position)
        {
            Position = position;
            speed = 500f;
            Active = true;
        }

        public void Move(float dt)
        {
            Position += new Vector2(speed * dt, 0);
            if (Position.X > 900)
                Active = false;
        }
    }

    public class BladeManager
    {
        public List<Blade> blades = new List<Blade>();

        public void SpawnBlade(Vector2 position)
        {
            blades.Add(new Blade(position));
        }

        public void Update(float dt)
        {
            foreach (var blade in blades.ToArray())
            {
                blade.Move(dt);
                if (!blade.Active)
                    blades.Remove(blade);
            }
        }

        public void CheckEnemyHit(List<Enemy> enemies)
        {
            foreach (var blade in blades.ToArray())
            {
                Rectangle br = new Rectangle((int)blade.Position.X, (int)blade.Position.Y, 20, 5);

                foreach (var enemy in enemies.ToArray())
                {
                    Rectangle er = new Rectangle((int)enemy.Position.X, (int)enemy.Position.Y, 35, 35);

                    if (br.Intersects(er))
                    {
                        enemy.TakeDamage(20);
                        blade.Active = false;
                        if (enemy.Health <= 0)
                            enemies.Remove(enemy);
                        break;
                    }
                }
            }
        }
    }

    public class UIManager
    {
        public MenuState CurrentMenu { get; private set; }
        public OptionsMenu OptionsMenu { get; private set; }

        public UIManager()
        {
            CurrentMenu = MenuState.MAIN_MENU;
            OptionsMenu = new OptionsMenu();
        }

        public void ShowMainMenu()
        {
            CurrentMenu = MenuState.MAIN_MENU;
        }

        public void ShowOptionMenu()
        {
            CurrentMenu = MenuState.OPTIONS_MENU;
        }
    }

    public class OptionsMenu
    {
        public MenuTab CurrentTab { get; private set; }
        public SoundSettingsTab SoundTab { get; private set; }
        public UISettingsTab UITab { get; private set; }
        private readonly List<MenuTab> tabs;

        public OptionsMenu()
        {
            tabs = new List<MenuTab>();
            SoundTab = new SoundSettingsTab("Sound");
            UITab = new UISettingsTab("UI");
            tabs.Add(SoundTab);
            tabs.Add(UITab);
            CurrentTab = SoundTab;
            CurrentTab.IsActive = true;
        }

        public void HandleTabNavigation(int dir)
        {
            int index = tabs.IndexOf(CurrentTab);
            if (index < 0)
                index = 0;

            CurrentTab.IsActive = false;
            index = Math.Clamp(index + dir, 0, tabs.Count - 1);
            CurrentTab = tabs[index];
            CurrentTab.IsActive = true;
        }
    }

    public abstract class MenuTab
    {
        public string TabName { get; protected set; }
        public bool IsActive { get; set; }

        protected MenuTab(string name)
        {
            TabName = name;
            IsActive = false;
        }
    }

    public class SoundSettingsTab : MenuTab
    {
        public float MasterVolume { get; private set; }
        public float MusicVolume { get; private set; }

        public SoundSettingsTab(string name) : base(name)
        {
            MasterVolume = 1f;
            MusicVolume = 1f;
        }

        public void OnVolumeChanged(float master, float music)
        {
            MasterVolume = MathHelper.Clamp(master, 0f, 1f);
            MusicVolume = MathHelper.Clamp(music, 0f, 1f);
        }
    }

    public class UISettingsTab : MenuTab
    {
        public bool ShowFPS { get; private set; }
        public float UIScale { get; private set; }

        public UISettingsTab(string name) : base(name)
        {
            ShowFPS = false;
            UIScale = 1f;
        }

        public void ApplyUISettings(bool showFps, float scale)
        {
            ShowFPS = showFps;
            UIScale = MathHelper.Clamp(scale, 0.5f, 2f);
        }
    }

    public class SoundManager
    {
        public float MasterVolume { get; private set; }
        public float MusicVolume { get; private set; }

        public SoundManager()
        {
            MasterVolume = 1f;
            MusicVolume = 1f;
        }

        public void SetVolumes(float master, float music)
        {
            MasterVolume = MathHelper.Clamp(master, 0f, 1f);
            MusicVolume = MathHelper.Clamp(music, 0f, 1f);
        }
    }
}
