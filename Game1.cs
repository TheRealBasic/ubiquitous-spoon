using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace NightclubSim
{
    public enum GameMode
    {
        Build,
        Live
    }

    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch = null!;
        private PixelFont _font = null!;
        private IsoRenderer _iso = new();
        private World _world = null!;
        private Texture2D _tileTexture = null!;
        private Texture2D _entityTexture = null!;
        private Texture2D _panelTexture = null!;

        private Economy _economy = new();
        private GameMode _mode = GameMode.Live;
        private bool _clubOpen = true;
        private float _timeOfDay = 18f; // start at evening
        private readonly List<string> _log = new();

        private readonly List<Customer> _customers = new();
        private readonly List<Staff> _staff = new();
        private readonly Random _rng = new();

        private double _spawnTimer = 0;
        private PlaceableType _selectedPlacement = PlaceableType.BarCounter;

        private MouseState _previousMouse;
        private KeyboardState _previousKeyboard;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.Title = "Chrome Pulse - Isometric Nightclub";
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.ApplyChanges();
            _world = new World(16, 12);
            _economy.Log += AddLog;
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _font = new PixelFont(GraphicsDevice);
            _tileTexture = TextureFactory.CreateDiamond(GraphicsDevice, _iso.TileWidth, _iso.TileHeight);
            _entityTexture = TextureFactory.CreateRectangle(GraphicsDevice, 12, 18, Color.White);
            _panelTexture = TextureFactory.CreateRectangle(GraphicsDevice, 1, 1, new Color(0, 0, 0, 0.5f));

            // Staff starting positions
            _staff.Add(new Staff(StaffRole.Bartender, new Vector2(_world.Width / 2, _world.Height / 2)));
            _staff.Add(new Staff(StaffRole.DJ, new Vector2(3, 3)));
            _staff.Add(new Staff(StaffRole.Bouncer, new Vector2(_world.Entrance.X, _world.Entrance.Y - 1)));

            if (SaveManager.TryLoad(_world, _economy, _staff))
            {
                AddLog("Loaded save data.");
            }
            else
            {
                AddLog("New game started.");
            }
        }

        protected override void Update(GameTime gameTime)
        {
            var keyboard = Keyboard.GetState();
            if (keyboard.IsKeyDown(Keys.Escape)) Exit();

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            HandleCameraInput(dt, keyboard);
            HandleModeInput(keyboard);

            var mouse = Mouse.GetState();
            HandleMouse(mouse);

            if (_mode == GameMode.Live)
            {
                UpdateTime(dt);
                UpdateCustomers(gameTime);
                SpawnCustomers(dt);
            }

            _previousMouse = mouse;
            _previousKeyboard = keyboard;
            base.Update(gameTime);
        }

        private void HandleCameraInput(float dt, KeyboardState keyboard)
        {
            const float speed = 200f;
            if (keyboard.IsKeyDown(Keys.A)) _iso.Camera.X += speed * dt;
            if (keyboard.IsKeyDown(Keys.D)) _iso.Camera.X -= speed * dt;
            if (keyboard.IsKeyDown(Keys.W)) _iso.Camera.Y += speed * dt;
            if (keyboard.IsKeyDown(Keys.S)) _iso.Camera.Y -= speed * dt;
        }

        private void HandleModeInput(KeyboardState keyboard)
        {
            if (keyboard.IsKeyDown(Keys.Tab) && !_previousKeyboard.IsKeyDown(Keys.Tab))
            {
                _mode = _mode == GameMode.Build ? GameMode.Live : GameMode.Build;
                AddLog($"Switched to {_mode} mode.");
            }
            if (keyboard.IsKeyDown(Keys.O) && !_previousKeyboard.IsKeyDown(Keys.O))
            {
                _clubOpen = !_clubOpen;
                if (!_clubOpen)
                {
                    foreach (var c in _customers) c.ForceLeave(_world);
                }
                AddLog(_clubOpen ? "Club opened." : "Club closed.");
            }
            if (keyboard.IsKeyDown(Keys.N) && !_previousKeyboard.IsKeyDown(Keys.N))
            {
                SaveManager.Clear();
                _economy.LoadState(500, 1, 0);
                _customers.Clear();
                _staff.Clear();
                _staff.Add(new Staff(StaffRole.Bartender, new Vector2(_world.Width / 2, _world.Height / 2)));
                _staff.Add(new Staff(StaffRole.DJ, new Vector2(3, 3)));
                _staff.Add(new Staff(StaffRole.Bouncer, new Vector2(_world.Entrance.X, _world.Entrance.Y - 1)));
                foreach (var tile in _world.Tiles())
                {
                    tile.PlacedObject = null;
                    if (tile.Type != TileType.Wall && tile.Type != TileType.Entrance)
                        tile.Type = TileType.Floor;
                }
                AddLog("New game created.");
            }
            if (keyboard.IsKeyDown(Keys.F5) && !_previousKeyboard.IsKeyDown(Keys.F5))
            {
                SaveManager.Save(_world, _economy, _staff);
                AddLog("Game saved.");
            }
        }

        private void HandleMouse(MouseState mouse)
        {
            var leftClick = mouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released;
            var rightClick = mouse.RightButton == ButtonState.Pressed && _previousMouse.RightButton == ButtonState.Released;

            var grid = _iso.ToGrid(new Vector2(mouse.X, mouse.Y));
            if (_mode == GameMode.Build)
            {
                if (leftClick)
                {
                    var placeable = Placeable.Create(_selectedPlacement);
                    if (_economy.Level < placeable.LevelRequirement)
                    {
                        AddLog("Level too low.");
                    }
                    else if (!_world.CanPlace(grid.X, grid.Y))
                    {
                        AddLog("Cannot build here.");
                    }
                    else if (!_economy.Spend(placeable.Cost))
                    {
                        AddLog("Not enough money.");
                    }
                    else
                    {
                        _world.Place(grid.X, grid.Y, placeable);
                        AddLog($"Placed {placeable.Name}.");
                    }
                }
                if (rightClick)
                {
                    if (_world.Remove(grid.X, grid.Y))
                    {
                        _economy.AddIncome(10); // small refund
                        AddLog("Item sold.");
                    }
                }
            }
        }

        private void UpdateTime(float dt)
        {
            _timeOfDay += dt * 0.25f; // slow clock
            if (_timeOfDay >= 24f) _timeOfDay -= 24f;
        }

        private void SpawnCustomers(double dt)
        {
            if (!_clubOpen) return;
            _spawnTimer -= dt;
            double spawnInterval = (_timeOfDay >= 18 || _timeOfDay <= 3) ? 2.0 : 5.0;
            spawnInterval /= 1 + _staff.FindAll(s => s.Role == StaffRole.Bouncer).Count * 0.2;
            if (_spawnTimer <= 0 && _customers.Count < 20)
            {
                var tile = _world.Entrance;
                var customer = new Customer(_rng, tile);
                _customers.Add(customer);
                AddLog("New customer entered.");
                _spawnTimer = spawnInterval;
            }
        }

        private void UpdateCustomers(GameTime gameTime)
        {
            float incomeThisFrame = 0f;
            for (int i = _customers.Count - 1; i >= 0; i--)
            {
                var c = _customers[i];
                c.Update(gameTime, _world);
                var tile = _world.GetTile((int)c.GridPosition.X, (int)c.GridPosition.Y);
                if (c.State == CustomerState.Using)
                {
                    float bonus = 1f;
                    if (tile.Type == TileType.Bar)
                        bonus += 0.25f * _staff.FindAll(s => s.Role == StaffRole.Bartender).Count;
                    if (tile.Type == TileType.DanceFloor)
                        bonus += 0.25f * _staff.FindAll(s => s.Role == StaffRole.DJ).Count;
                    if (tile.Type == TileType.Table)
                        bonus += 0.1f * _staff.Count;
                    incomeThisFrame += bonus * (tile.Type == TileType.Bar ? 5 : tile.Type == TileType.DanceFloor ? 3 : 2) * (float)gameTime.ElapsedGameTime.TotalSeconds;
                }
                if (c.State == CustomerState.Leaving && c.ReachedTarget())
                {
                    _customers.RemoveAt(i);
                }
            }
            if (incomeThisFrame > 0)
            {
                _economy.AddIncome((int)Math.Round(incomeThisFrame));
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(15, 15, 25));
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            foreach (var tile in _world.Tiles())
            {
                _iso.DrawTile(_spriteBatch, _tileTexture, tile);
                if (tile.PlacedObject != null)
                {
                    var pos = _iso.ToScreen(tile.GridPosition.X, tile.GridPosition.Y);
                    _spriteBatch.Draw(_tileTexture, pos, null, tile.PlacedObject.Color, 0f, new Vector2(_tileTexture.Width / 2f, _tileTexture.Height / 2f), _iso.Zoom * 0.9f, SpriteEffects.None, 0f);
                }
            }

            foreach (var staff in _staff)
            {
                var pos = _iso.ToScreen((int)staff.GridPosition.X, (int)staff.GridPosition.Y) - new Vector2(0, 10);
                _spriteBatch.Draw(_entityTexture, pos, null, staff.Color, 0f, new Vector2(_entityTexture.Width / 2f, _entityTexture.Height), _iso.Zoom, SpriteEffects.None, 0f);
            }

            foreach (var c in _customers)
            {
                var pos = _iso.ToScreen((int)c.GridPosition.X, (int)c.GridPosition.Y) - new Vector2(0, 8);
                _spriteBatch.Draw(_entityTexture, pos, null, Color.Cyan, 0f, new Vector2(_entityTexture.Width / 2f, _entityTexture.Height), _iso.Zoom, SpriteEffects.None, 0f);
            }

            DrawUI();

            _spriteBatch.End();
            base.Draw(gameTime);
        }

        private void DrawUI()
        {
            _spriteBatch.Draw(_panelTexture, new Rectangle(0, 0, _graphics.PreferredBackBufferWidth, 80), new Color(0, 0, 0, 150));
            _spriteBatch.Draw(_panelTexture, new Rectangle(0, 80, 220, _graphics.PreferredBackBufferHeight - 80), new Color(0, 0, 0, 100));

            _font.DrawString(_spriteBatch, $"MONEY: ${_economy.Money}", new Vector2(10, 10), Color.LimeGreen, 2f);
            _font.DrawString(_spriteBatch, $"XP: {_economy.Experience}/{_economy.ExperienceToNext} LVL {_economy.Level}", new Vector2(10, 30), Color.LightBlue, 2f);
            string dayState = (_timeOfDay >= 6 && _timeOfDay < 18) ? "DAY" : "NIGHT";
            _font.DrawString(_spriteBatch, $"TIME: {(int)_timeOfDay:00}:00 {dayState}", new Vector2(10, 50), Color.Gold, 2f);
            _font.DrawString(_spriteBatch, $"MODE: {_mode} | CLUB: {(_clubOpen ? "OPEN" : "CLOSED")}", new Vector2(260, 10), Color.White, 2f);
            _font.DrawString(_spriteBatch, "TAB SWITCH, O OPEN, N NEW, F5 SAVE", new Vector2(260, 30), Color.LightGray, 1.5f);

            DrawShop(new Vector2(10, 90));
            DrawLog(new Vector2(10, _graphics.PreferredBackBufferHeight - 100));
        }

        private void DrawShop(Vector2 start)
        {
            _font.DrawString(_spriteBatch, "BUILD ITEMS", start, Color.White, 2f);
            int index = 0;
            foreach (PlaceableType type in Enum.GetValues(typeof(PlaceableType)))
            {
                if (type == PlaceableType.None) continue;
                var item = Placeable.Create(type);
                string locked = _economy.Level < item.LevelRequirement ? " (LOCK)" : string.Empty;
                var line = $"[{index + 1}] {item.Name} ${item.Cost} L{item.LevelRequirement}{locked}";
                var pos = start + new Vector2(0, 20 + index * 18);
                var color = type == _selectedPlacement ? Color.Yellow : Color.LightGray;
                _font.DrawString(_spriteBatch, line, pos, color, 1.5f);
                index++;
            }
            var keyboard = Keyboard.GetState();
            for (int i = 0; i < index; i++)
            {
                Keys key = Keys.D1 + i;
                if (keyboard.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key))
                {
                    _selectedPlacement = (PlaceableType)(i + 1);
                    AddLog($"Selected {_selectedPlacement}.");
                }
            }
        }

        private void DrawLog(Vector2 start)
        {
            _font.DrawString(_spriteBatch, "LOG", start, Color.White, 2f);
            for (int i = 0; i < _log.Count; i++)
            {
                _font.DrawString(_spriteBatch, _log[i], start + new Vector2(0, 18 + i * 12), Color.LightGray, 1.2f);
            }
        }

        private void AddLog(string message)
        {
            _log.Add(message);
            if (_log.Count > 6) _log.RemoveAt(0);
        }
    }
}
