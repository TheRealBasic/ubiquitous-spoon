using System;
using System.Collections.Generic;
using System.Linq;
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

    public enum BuildTab
    {
        Items,
        Staff
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
        private GameMode _mode = GameMode.Build;
        private BuildTab _buildTab = BuildTab.Items;
        private bool _clubOpen = true;
        private float _timeOfDay = 18f; // start at evening
        private readonly List<string> _log = new();

        private readonly List<Customer> _customers = new();
        private readonly List<Staff> _staff = new();
        private readonly List<FloatingText> _floatingTexts = new();
        private readonly Random _rng = new();

        private double _spawnTimer = 0;
        private PlaceableType _selectedPlacement = PlaceableType.BarCounter;
        private StaffRole _selectedStaff = StaffRole.Bartender;
        private float _incomeBuffer;
        private float _xpBuffer;
        private float _autosaveTimer = 60f;
        private bool _cameraDragging;
        private bool _rightClickConsumed;
        private Vector2 _dragStart;
        private Vector2 _cameraStart;
        private bool _showDebug;

        private int _sessionGuests;
        private float _sessionSatisfactionSum;
        private int _sessionMoneyEarned;
        private float _clubRating = 1f;
        private float _prevTime;

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
            HandleBuildSelectionInput(keyboard);

            var mouse = Mouse.GetState();
            HandleMouse(mouse);

            if (_mode == GameMode.Live)
            {
                UpdateTime(dt);
                UpdateCustomers(gameTime);
                SpawnCustomers(dt);
            }

            UpdateFloatingTexts(dt);

            _autosaveTimer -= dt;
            if (_autosaveTimer <= 0f)
            {
                SaveManager.Save(_world, _economy, _staff);
                AddLog("Auto-saved.");
                _autosaveTimer = 60f;
            }

            _previousMouse = mouse;
            _previousKeyboard = keyboard;
            base.Update(gameTime);
        }

        private void HandleCameraInput(float dt, KeyboardState keyboard)
        {
            const float speed = 200f;
            var camera = _iso.Camera;
            if (keyboard.IsKeyDown(Keys.A)) camera.X += speed * dt;
            if (keyboard.IsKeyDown(Keys.D)) camera.X -= speed * dt;
            if (keyboard.IsKeyDown(Keys.W)) camera.Y += speed * dt;
            if (keyboard.IsKeyDown(Keys.S)) camera.Y -= speed * dt;
            _iso.Camera = camera;
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
                _floatingTexts.Clear();
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
                _sessionGuests = 0;
                _sessionMoneyEarned = 0;
                _sessionSatisfactionSum = 0;
                AddLog("New game created.");
            }
            if (keyboard.IsKeyDown(Keys.F5) && !_previousKeyboard.IsKeyDown(Keys.F5))
            {
                SaveManager.Save(_world, _economy, _staff);
                AddLog("Game saved.");
            }
            if (keyboard.IsKeyDown(Keys.F1) && !_previousKeyboard.IsKeyDown(Keys.F1))
            {
                _showDebug = !_showDebug;
            }
            if (keyboard.IsKeyDown(Keys.F2) && !_previousKeyboard.IsKeyDown(Keys.F2))
            {
                _buildTab = _buildTab == BuildTab.Items ? BuildTab.Staff : BuildTab.Items;
            }
        }

        private void HandleBuildSelectionInput(KeyboardState keyboard)
        {
            if (_mode != GameMode.Build) return;
            if (_buildTab == BuildTab.Items)
            {
                int itemCount = Enum.GetValues(typeof(PlaceableType)).Length - 1;
                for (int i = 0; i < itemCount; i++)
                {
                    Keys key = Keys.D1 + i;
                    if (keyboard.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key))
                    {
                        _selectedPlacement = (PlaceableType)(i + 1);
                        AddLog($"Selected {_selectedPlacement}.");
                    }
                }
                if (keyboard.IsKeyDown(Keys.Q) && !_previousKeyboard.IsKeyDown(Keys.Q))
                {
                    CyclePlacement(-1, itemCount);
                }
                if (keyboard.IsKeyDown(Keys.E) && !_previousKeyboard.IsKeyDown(Keys.E))
                {
                    CyclePlacement(1, itemCount);
                }
            }
            else
            {
                StaffRole[] roles = { StaffRole.Bartender, StaffRole.DJ, StaffRole.Bouncer };
                for (int i = 0; i < roles.Length; i++)
                {
                    Keys key = Keys.D1 + i;
                    if (keyboard.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key))
                    {
                        _selectedStaff = roles[i];
                        AddLog($"Hiring {_selectedStaff} selected.");
                    }
                }
            }
        }

        private void CyclePlacement(int direction, int itemCount)
        {
            int idx = (int)_selectedPlacement - 1;
            idx = (idx + direction + itemCount) % itemCount;
            _selectedPlacement = (PlaceableType)(idx + 1);
            AddLog($"Selected {_selectedPlacement}.");
        }

        private void HandleBuildSelectionInput(KeyboardState keyboard)
        {
            if (_mode != GameMode.Build) return;
            int itemCount = Enum.GetValues(typeof(PlaceableType)).Length - 1;
            for (int i = 0; i < itemCount; i++)
            {
                Keys key = Keys.D1 + i;
                if (keyboard.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key))
                {
                    _selectedPlacement = (PlaceableType)(i + 1);
                    AddLog($"Selected {_selectedPlacement}.");
                }
            }
            if (keyboard.IsKeyDown(Keys.Q) && !_previousKeyboard.IsKeyDown(Keys.Q))
            {
                CyclePlacement(-1, itemCount);
            }
            if (keyboard.IsKeyDown(Keys.E) && !_previousKeyboard.IsKeyDown(Keys.E))
            {
                CyclePlacement(1, itemCount);
            }
        }

        private void CyclePlacement(int direction, int itemCount)
        {
            int idx = (int)_selectedPlacement - 1;
            idx = (idx + direction + itemCount) % itemCount;
            _selectedPlacement = (PlaceableType)(idx + 1);
            AddLog($"Selected {_selectedPlacement}.");
        }

        private void HandleMouse(MouseState mouse)
        {
            var mousePos = new Vector2(mouse.X, mouse.Y);
            var grid = _iso.ToGrid(mousePos);

            int scrollDelta = mouse.ScrollWheelValue - _previousMouse.ScrollWheelValue;
            if (scrollDelta != 0)
            {
                _iso.Zoom = MathHelper.Clamp(_iso.Zoom + scrollDelta * 0.0015f, 0.6f, 1.8f);
            }

            if (mouse.RightButton == ButtonState.Pressed)
            {
                if (_previousMouse.RightButton == ButtonState.Released)
                {
                    _cameraDragging = true;
                    _rightClickConsumed = false;
                    _dragStart = mousePos;
                    _cameraStart = _iso.Camera;
                }
                else if (_cameraDragging)
                {
                    var delta = mousePos - _dragStart;
                    if (delta.LengthSquared() > 25f) _rightClickConsumed = true;
                    _iso.Camera = _cameraStart + delta;
                }
            }
            else if (_previousMouse.RightButton == ButtonState.Pressed)
            {
                bool treatAsClick = !_rightClickConsumed && _mode == GameMode.Build;
                _cameraDragging = false;
                if (treatAsClick)
                {
                    if (_world.Remove(grid.X, grid.Y))
                    {
                        _economy.AddIncome(10); // small refund
                        AddLog("Item sold.");
                    }
                }
            }

            var leftClick = mouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released;
            if (_mode == GameMode.Build && leftClick)
            {
                if (_buildTab == BuildTab.Items)
            {
                _iso.Zoom = MathHelper.Clamp(_iso.Zoom + scrollDelta * 0.0015f, 0.6f, 1.8f);
            }

            if (mouse.RightButton == ButtonState.Pressed)
            {
                if (_previousMouse.RightButton == ButtonState.Released)
                {
                    _cameraDragging = true;
                    _rightClickConsumed = false;
                    _dragStart = mousePos;
                    _cameraStart = _iso.Camera;
                }
                else
                else if (_cameraDragging)
                {
                    var delta = mousePos - _dragStart;
                    if (delta.LengthSquared() > 25f) _rightClickConsumed = true;
                    _iso.Camera = _cameraStart + delta;
                }
            }
            else if (_previousMouse.RightButton == ButtonState.Pressed)
            {
                bool treatAsClick = !_rightClickConsumed && _mode == GameMode.Build;
                _cameraDragging = false;
                if (treatAsClick)
                {
                    if (!_world.IsInside(grid.X, grid.Y)) return;
                    var staffTemplate = new Staff(_selectedStaff, Vector2.Zero);
                    if (!_economy.Spend(staffTemplate.HireCost))
                    {
                        AddLog("Not enough money to hire.");
                        return;
                    }
                    if (!IsValidStaffPlacement(_selectedStaff, grid))
                    {
                        AddLog("Invalid spot for staff.");
                        _economy.AddIncome(staffTemplate.HireCost); // refund immediately
                        return;
                    }
                    _staff.Add(new Staff(_selectedStaff, new Vector2(grid.X, grid.Y)));
                    AddLog($"Hired {_selectedStaff}.");
                }
            }

            var leftClick = mouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released;
            if (_mode == GameMode.Build && leftClick)
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
        }

        private bool IsValidStaffPlacement(StaffRole role, Point grid)
        {
            if (!_world.IsInside(grid.X, grid.Y)) return false;
            var tile = _world.GetTile(grid.X, grid.Y);
            if (!tile.Walkable) return false;
            if (role == StaffRole.Bouncer)
            {
                return Math.Abs(grid.X - _world.Entrance.X) + Math.Abs(grid.Y - _world.Entrance.Y) <= 2;
            }
            foreach (var offset in new[] { new Point(0, 0), new Point(1, 0), new Point(-1, 0), new Point(0, 1), new Point(0, -1) })
            {
                int nx = grid.X + offset.X;
                int ny = grid.Y + offset.Y;
                if (!_world.IsInside(nx, ny)) continue;
                var t = _world.GetTile(nx, ny);
                if (role == StaffRole.Bartender && t.Type == TileType.Bar) return true;
                if (role == StaffRole.DJ && t.Type == TileType.DanceFloor) return true;
            }
            return false;
        }

        private void UpdateTime(float dt)
        {
            _prevTime = _timeOfDay;
            _timeOfDay += dt * 0.25f; // slow clock
            if (_timeOfDay >= 24f)
            {
                _timeOfDay -= 24f;
            }

            if (_prevTime > _timeOfDay)
            {
                float avgSat = _sessionGuests > 0 ? _sessionSatisfactionSum / _sessionGuests : 50f;
                AddLog($"Night end: Guests {_sessionGuests}, Avg Sat {(int)avgSat}, Money ${_sessionMoneyEarned}.");
                _clubRating = ComputeRating(avgSat);
                _sessionGuests = 0;
                _sessionSatisfactionSum = 0;
                _sessionMoneyEarned = 0;
            }
        }

        private void SpawnCustomers(double dt)
        {
            if (!_clubOpen) return;
            _spawnTimer -= dt;
            double spawnInterval = (_timeOfDay >= 18 || _timeOfDay <= 3) ? 2.0 : 5.0;
            int djCount = _staff.Count(s => s.Role == StaffRole.DJ);
            spawnInterval /= 1 + djCount * 0.15;
            if (_spawnTimer <= 0 && _customers.Count < MaxCustomerCount())
            {
                var tile = _world.Entrance;
                var customer = new Customer(_rng, tile);
                _customers.Add(customer);
                AddLog("New customer entered.");
                _sessionGuests++;
                _spawnTimer = spawnInterval;
            }
        }

        private int MaxCustomerCount()
        {
            int bouncers = _staff.Count(s => s.Role == StaffRole.Bouncer);
            return 18 + bouncers * 5;
        }

        private void UpdateCustomers(GameTime gameTime)
        {
            float incomeThisFrame = 0f;
            float xpThisFrame = 0f;
            float satisfactionSum = 0f;
            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            for (int i = _customers.Count - 1; i >= 0; i--)
            {
                var c = _customers[i];
                c.Update(gameTime, _world);
                var tile = _world.GetTile((int)c.GridPosition.X, (int)c.GridPosition.Y);
                bool spending = false;
                if (c.State == CustomerState.Using)
                {
                    float bonus = 1f;
                    if (tile.Type == TileType.Bar)
                        bonus += 0.3f * _staff.FindAll(s => s.Role == StaffRole.Bartender).Count;
                    if (tile.Type == TileType.DanceFloor)
                        bonus += 0.3f * _staff.FindAll(s => s.Role == StaffRole.DJ).Count;
                    if (tile.Type == TileType.Table)
                        bonus += 0.1f * _staff.Count;
                    float earnRate = (tile.Type == TileType.Bar ? 5 : tile.Type == TileType.DanceFloor ? 3 : 2);
                    incomeThisFrame += bonus * earnRate * dt;
                    xpThisFrame += (bonus * 0.5f) * dt;
                    spending = true;
                }
                if (spending && _rng.NextDouble() < 0.15 * dt)
                {
                    var screenPos = _iso.ToScreen((int)c.SmoothPosition.X, (int)c.SmoothPosition.Y) - new Vector2(0, 30);
                    _floatingTexts.Add(new FloatingText($"+${_rng.Next(6, 15)}", screenPos, Color.LimeGreen, 1.2f, 1.2f));
                    incomeThisFrame += bonus * earnRate * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    xpThisFrame += (bonus * 0.5f) * (float)gameTime.ElapsedGameTime.TotalSeconds;
                }
                if (c.State == CustomerState.Leaving && c.ReachedTarget())
                {
                    if (c.FrustratedLeave)
                    {
                        AddLog("A guest left unhappy.");
                    }
                    else
                    {
                        _floatingTexts.Add(new FloatingText("Thanks!", _iso.ToScreen((int)c.SmoothPosition.X, (int)c.SmoothPosition.Y) - new Vector2(0, 26), Color.LightBlue, 1.3f, 1f));
                    }
                    _sessionSatisfactionSum += c.Satisfaction;
                    _customers.RemoveAt(i);
                }
                satisfactionSum += c.Satisfaction;
            }

            _incomeBuffer += incomeThisFrame;
            _xpBuffer += xpThisFrame;
            int incomeDelta = (int)Math.Floor(_incomeBuffer);
            if (incomeDelta > 0)
            {
                _economy.AddIncome(incomeDelta);
                _sessionMoneyEarned += incomeDelta;
                _incomeBuffer -= incomeDelta;
                _floatingTexts.Add(new FloatingText($"+${incomeDelta}", new Vector2(220, 20), Color.LimeGreen, 1f, 1.1f));
            }

            _incomeBuffer += incomeThisFrame;
            _xpBuffer += xpThisFrame;
            int incomeDelta = (int)Math.Floor(_incomeBuffer);
            if (incomeDelta > 0)
            {
                _economy.AddIncome(incomeDelta);
                _incomeBuffer -= incomeDelta;
            }
            int xpDelta = (int)Math.Floor(_xpBuffer);
            if (xpDelta > 0)
            {
                _economy.GrantXp(xpDelta);
                _xpBuffer -= xpDelta;
            }

            float overcrowdingPenalty = Math.Max(0, _customers.Count - MaxCustomerCount() + 5) * 0.5f;
            foreach (var c in _customers)
            {
                c.Satisfaction = MathHelper.Clamp(c.Satisfaction - overcrowdingPenalty * dt, 0f, 100f);
            }

            if (_customers.Count > 0)
            {
                float avg = satisfactionSum / _customers.Count;
                _clubRating = MathHelper.Lerp(_clubRating, ComputeRating(avg), 0.05f);
            }
        }

        private void UpdateFloatingTexts(float dt)
        {
            for (int i = _floatingTexts.Count - 1; i >= 0; i--)
            {
                if (_floatingTexts[i].Update(dt))
                {
                    _floatingTexts.RemoveAt(i);
                }
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(20, 18, 30));
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            var mouse = Mouse.GetState();
            DrawWorld(mouse);
            DrawFloatingTexts();
            DrawUI(mouse);
            DrawVignette();

            _spriteBatch.End();
            base.Draw(gameTime);
        }

        private void DrawWorld(MouseState mouse)
        {
            var gridHover = _iso.ToGrid(new Vector2(mouse.X, mouse.Y));
            foreach (var tile in _world.Tiles())
            {
                _iso.DrawTile(_spriteBatch, _tileTexture, tile);
                if (tile.PlacedObject != null)
                {
                    var pos = _iso.ToScreen(tile.GridPosition.X, tile.GridPosition.Y);
                    _spriteBatch.Draw(_tileTexture, pos, null, tile.PlacedObject.Color, 0f, new Vector2(_tileTexture.Width / 2f, _tileTexture.Height / 2f), _iso.Zoom * 0.9f, SpriteEffects.None, 0f);
                }
            }

            // Placement highlight
            if (_mode == GameMode.Build && _world.IsInside(gridHover.X, gridHover.Y))
            {
                DrawBuildPreview(mouse);
                var pos = _iso.ToScreen(gridHover.X, gridHover.Y);
                _spriteBatch.Draw(_tileTexture, pos, null, new Color(255, 255, 0, 80), 0f, new Vector2(_tileTexture.Width / 2f, _tileTexture.Height / 2f), _iso.Zoom * 1.05f, SpriteEffects.None, 0f);
            }

            // Staff
            foreach (var staff in _staff)
            {
                var pos = _iso.ToScreen((int)staff.GridPosition.X, (int)staff.GridPosition.Y) - new Vector2(0, 6);
                DrawShadow(pos + new Vector2(0, 16));
                _spriteBatch.Draw(_entityTexture, pos, null, staff.Color, 0f, new Vector2(_entityTexture.Width / 2f, _entityTexture.Height), _iso.Zoom, SpriteEffects.None, 0f);
            }

            Customer? hovered = null;
            float closest = 18f;
            foreach (var c in _customers)
            {
                var pos = _iso.ToScreen((int)c.SmoothPosition.X, (int)c.SmoothPosition.Y) - new Vector2(0, 8);
                DrawShadow(pos + new Vector2(0, 18));
                _spriteBatch.Draw(_entityTexture, pos, null, Color.Cyan, 0f, new Vector2(_entityTexture.Width / 2f, _entityTexture.Height), _iso.Zoom, SpriteEffects.None, 0f);
                if (c.IsSelected)
                {
                    _spriteBatch.Draw(_tileTexture, _iso.ToScreen((int)c.GridPosition.X, (int)c.GridPosition.Y), null, new Color(0, 200, 255, 80), 0f, new Vector2(_tileTexture.Width / 2f, _tileTexture.Height / 2f), _iso.Zoom * 1.1f, SpriteEffects.None, 0f);
                }

                float dist = Vector2.Distance(pos, new Vector2(mouse.X, mouse.Y));
                if (dist < closest)
                {
                    closest = dist;
                    hovered = c;
                }

                if (c.ShouldShowThought())
                {
                    var bubblePos = pos - new Vector2(0, 24);
                    Color bubbleColor = c.PreferredActivity == CustomerPreference.Dance ? Color.MediumPurple : Color.SandyBrown;
                    if (c.Satisfaction < 30) bubbleColor = Color.DarkSlateGray;
                    _spriteBatch.Draw(_panelTexture, new Rectangle((int)bubblePos.X - 6, (int)bubblePos.Y - 6, 16, 12), bubbleColor * 0.8f);
                    _spriteBatch.Draw(_panelTexture, new Rectangle((int)bubblePos.X - 4, (int)bubblePos.Y - 4, 12, 8), Color.White * 0.6f);
                    c.ResetThoughtTimer();
                }
            }

            foreach (var c in _customers) c.IsSelected = false;
            if (hovered != null) hovered.IsSelected = true;
        }
            DrawBuildPreview(Mouse.GetState());
            DrawUI();

        private void DrawFloatingTexts()
        {
            foreach (var ft in _floatingTexts)
            {
                _font.DrawString(_spriteBatch, ft.Text, ft.Position, ft.Color * ft.Opacity, ft.Scale);
            }
        }

        private void DrawUI(MouseState mouse)
        {
            _spriteBatch.Draw(_panelTexture, new Rectangle(0, 0, _graphics.PreferredBackBufferWidth, 82), new Color(0, 0, 0, 170));
            _spriteBatch.Draw(_panelTexture, new Rectangle(0, 80, 240, _graphics.PreferredBackBufferHeight - 80), new Color(0, 0, 0, 110));

            Color modeColor = _mode == GameMode.Build ? Color.Gold : Color.LimeGreen;
            _font.DrawString(_spriteBatch, $"MONEY: ${_economy.Money}", new Vector2(10, 10), Color.LimeGreen, 2f);
            _font.DrawString(_spriteBatch, $"XP: {_economy.Experience}/{_economy.ExperienceToNext} LVL {_economy.Level}", new Vector2(10, 30), Color.LightBlue, 2f);
            string dayState = (_timeOfDay >= 6 && _timeOfDay < 18) ? "DAY" : "NIGHT";
            _font.DrawString(_spriteBatch, $"TIME: {(int)_timeOfDay:00}:00 {dayState}", new Vector2(10, 50), Color.Gold, 2f);
            _font.DrawString(_spriteBatch, $"MODE: {_mode} | CLUB: {(_clubOpen ? "OPEN" : "CLOSED")}", new Vector2(260, 10), modeColor, 2f);
            _font.DrawString(_spriteBatch, "TAB SWITCH, O OPEN, N NEW, F5 SAVE | F2 STAFF", new Vector2(260, 30), Color.LightGray, 1.5f);
            _font.DrawString(_spriteBatch, $"GUESTS: {_customers.Count} | STAFF: {_staff.Count} | RATING: {_clubRating:0.0}", new Vector2(260, 50), Color.LightSeaGreen, 1.5f);
            _font.DrawString(_spriteBatch, $"MODE: {_mode} | CLUB: {(_clubOpen ? "OPEN" : "CLOSED")}", new Vector2(260, 10), Color.White, 2f);
            _font.DrawString(_spriteBatch, "TAB SWITCH, O OPEN, N NEW, F5 SAVE", new Vector2(260, 30), Color.LightGray, 1.5f);
            _font.DrawString(_spriteBatch, $"GUESTS: {_customers.Count} | STAFF: {_staff.Count} | RATING: {ComputeRating():0.0}", new Vector2(260, 50), Color.LightSeaGreen, 1.5f);

            if (_mode == GameMode.Build)
            {
                DrawShop(new Vector2(10, 90));
            }
            else
            {
                _font.DrawString(_spriteBatch, "Build items hidden (TAB to edit)", new Vector2(10, 90), Color.LightGray, 1.5f);
            }
            DrawLog(new Vector2(10, _graphics.PreferredBackBufferHeight - 140));
            DrawTileTooltip(Mouse.GetState(), new Vector2(10, _graphics.PreferredBackBufferHeight - 170));
            DrawSelectedCustomerInfo(new Vector2(_graphics.PreferredBackBufferWidth - 220, 90));

            if (_showDebug)
            {
                DrawDebugPanel(new Vector2(_graphics.PreferredBackBufferWidth - 220, 10));
            }
            DrawLog(new Vector2(10, _graphics.PreferredBackBufferHeight - 120));
            DrawTileTooltip(Mouse.GetState(), new Vector2(10, _graphics.PreferredBackBufferHeight - 150));
        }

        private void DrawShop(Vector2 start)
        {
            _font.DrawString(_spriteBatch, _buildTab == BuildTab.Items ? "BUILD ITEMS" : "STAFF", start, Color.White, 2f);
            if (_buildTab == BuildTab.Items)
            {
                int index = 0;
                foreach (PlaceableType type in Enum.GetValues(typeof(PlaceableType)))
                {
                    if (type == PlaceableType.None) continue;
                    var item = Placeable.Create(type);
                    string locked = _economy.Level < item.LevelRequirement ? " (LOCK)" : string.Empty;
                    var line = $"[{index + 1}] {item.Name} ${item.Cost} L{item.LevelRequirement}{locked}";
                    var pos = start + new Vector2(0, 22 + index * 18);
                    var color = type == _selectedPlacement ? Color.Yellow : Color.LightGray;
                    _font.DrawString(_spriteBatch, line, pos, color, 1.5f);
                    index++;
                }
                var selected = Placeable.Create(_selectedPlacement);
                _font.DrawString(_spriteBatch, $"Selected: {selected.Name}", start + new Vector2(0, 22 + index * 18), Color.LightGoldenrodYellow, 1.5f);
                _font.DrawString(_spriteBatch, $"Cost: ${selected.Cost} | Needs L{selected.LevelRequirement}", start + new Vector2(0, 40 + index * 18), Color.LightGoldenrodYellow, 1.3f);
            }
            else
            {
                StaffRole[] roles = { StaffRole.Bartender, StaffRole.DJ, StaffRole.Bouncer };
                for (int i = 0; i < roles.Length; i++)
                {
                    var staff = new Staff(roles[i], Vector2.Zero);
                    var line = $"[{i + 1}] {roles[i]} ${staff.HireCost}";
                    var pos = start + new Vector2(0, 22 + i * 18);
                    var color = roles[i] == _selectedStaff ? Color.Yellow : Color.LightGray;
                    _font.DrawString(_spriteBatch, line, pos, color, 1.5f);
                }
                _font.DrawString(_spriteBatch, "Place staff next to valid tiles", start + new Vector2(0, 90), Color.LightGoldenrodYellow, 1.2f);
            var selected = Placeable.Create(_selectedPlacement);
            _font.DrawString(_spriteBatch, $"Selected: {selected.Name}", start + new Vector2(0, 20 + index * 18), Color.LightGoldenrodYellow, 1.5f);
            _font.DrawString(_spriteBatch, $"Cost: ${selected.Cost} | Needs L{selected.LevelRequirement}", start + new Vector2(0, 38 + index * 18), Color.LightGoldenrodYellow, 1.3f);
        }

        private void DrawTileTooltip(MouseState mouse, Vector2 start)
        {
            var grid = _iso.ToGrid(new Vector2(mouse.X, mouse.Y));
            if (!_world.IsInside(grid.X, grid.Y)) return;
            var tile = _world.GetTile(grid.X, grid.Y);
            string occupant = tile.PlacedObject != null ? $" ({tile.PlacedObject.Name})" : string.Empty;
            _font.DrawString(_spriteBatch, $"Hover: {tile.Type}{occupant}", start, Color.LightSteelBlue, 1.2f);
        }

        private void DrawBuildPreview(MouseState mouse)
        {
            if (_mode != GameMode.Build) return;
            var grid = _iso.ToGrid(new Vector2(mouse.X, mouse.Y));
            if (!_world.IsInside(grid.X, grid.Y)) return;
            var placeable = Placeable.Create(_selectedPlacement);
            bool valid = _economy.Level >= placeable.LevelRequirement && _world.CanPlace(grid.X, grid.Y) && _economy.Money >= placeable.Cost;
            var pos = _iso.ToScreen(grid.X, grid.Y);
            var color = (valid ? Color.LimeGreen : Color.Red) * 0.4f;
            _spriteBatch.Draw(_tileTexture, pos, null, color, 0f, new Vector2(_tileTexture.Width / 2f, _tileTexture.Height / 2f), _iso.Zoom * 1.05f, SpriteEffects.None, 0f);
        }

        private float ComputeRating()
        {
            int decor = 0, dance = 0, bar = 0;
            foreach (var tile in _world.Tiles())
            {
                if (tile.Type == TileType.Decor) decor++;
                if (tile.Type == TileType.DanceFloor) dance++;
                if (tile.Type == TileType.Bar) bar++;
            }
            float rating = 1f + decor * 0.05f + dance * 0.07f + bar * 0.05f + _staff.Count * 0.05f;
            return MathHelper.Clamp(rating, 1f, 5f);
        }

        private void DrawTileTooltip(MouseState mouse, Vector2 start)
        {
            var grid = _iso.ToGrid(new Vector2(mouse.X, mouse.Y));
            if (!_world.IsInside(grid.X, grid.Y)) return;
            var tile = _world.GetTile(grid.X, grid.Y);
            string occupant = tile.PlacedObject != null ? $" ({tile.PlacedObject.Name})" : string.Empty;
            _font.DrawString(_spriteBatch, $"Hover: {tile.Type}{occupant}", start, Color.LightSteelBlue, 1.2f);
        }

        private void DrawBuildPreview(MouseState mouse)
        {
            if (_mode != GameMode.Build) return;
            if (_buildTab != BuildTab.Items) return;
            var grid = _iso.ToGrid(new Vector2(mouse.X, mouse.Y));
            if (!_world.IsInside(grid.X, grid.Y)) return;
            var placeable = Placeable.Create(_selectedPlacement);
            bool valid = _economy.Level >= placeable.LevelRequirement && _world.CanPlace(grid.X, grid.Y) && _economy.Money >= placeable.Cost;
            var pos = _iso.ToScreen(grid.X, grid.Y);
            var color = (valid ? Color.LimeGreen : Color.Red) * 0.4f;
            _spriteBatch.Draw(_tileTexture, pos, null, color, 0f, new Vector2(_tileTexture.Width / 2f, _tileTexture.Height / 2f), _iso.Zoom * 1.05f, SpriteEffects.None, 0f);
        }

        private float ComputeRating(float avgSatisfaction)
        {
            int decor = 0, dance = 0, bar = 0;
            foreach (var tile in _world.Tiles())
            {
                if (tile.Type == TileType.Decor) decor++;
                if (tile.Type == TileType.DanceFloor) dance++;
                if (tile.Type == TileType.Bar) bar++;
            }
            float variety = MathHelper.Clamp(1f + decor * 0.03f + dance * 0.05f + bar * 0.04f, 1f, 5f);
            float crowdPenalty = Math.Max(0, _customers.Count - MaxCustomerCount() + 5) * 0.05f;
            float rating = (avgSatisfaction / 25f) + variety - crowdPenalty;
            return MathHelper.Clamp(rating, 0f, 5f);
        }

        private void DrawLog(Vector2 start)
        {
            _font.DrawString(_spriteBatch, "LOG", start, Color.White, 2f);
            for (int i = 0; i < _log.Count; i++)
            {
                _font.DrawString(_spriteBatch, _log[i], start + new Vector2(0, 18 + i * 12), Color.LightGray, 1.2f);
            }
        }

        private void DrawSelectedCustomerInfo(Vector2 start)
        {
            var selected = _customers.FirstOrDefault(c => c.IsSelected);
            if (selected == null) return;
            _spriteBatch.Draw(_panelTexture, new Rectangle((int)start.X - 6, (int)start.Y - 8, 210, 90), new Color(0, 0, 0, 170));
            _font.DrawString(_spriteBatch, "GUEST", start, Color.White, 1.8f);
            _font.DrawString(_spriteBatch, $"Sat: {(int)selected.Satisfaction}", start + new Vector2(0, 18), Color.LightGreen, 1.5f);
            _font.DrawString(_spriteBatch, selected.GetMoodDescription(), start + new Vector2(0, 34), Color.LightGray, 1.2f);
            _font.DrawString(_spriteBatch, $"Activity: {selected.CurrentActivity}", start + new Vector2(0, 50), Color.LightSteelBlue, 1.2f);
            _font.DrawString(_spriteBatch, $"Pref: {selected.PreferredActivity}", start + new Vector2(0, 66), Color.LightSteelBlue, 1.2f);
        }

        private void DrawDebugPanel(Vector2 start)
        {
            int bartenders = _staff.Count(s => s.Role == StaffRole.Bartender);
            int djs = _staff.Count(s => s.Role == StaffRole.DJ);
            int bouncers = _staff.Count(s => s.Role == StaffRole.Bouncer);
            float avgSat = _customers.Count > 0 ? _customers.Average(c => c.Satisfaction) : 0f;
            _spriteBatch.Draw(_panelTexture, new Rectangle((int)start.X - 6, (int)start.Y - 6, 210, 80), new Color(0, 0, 0, 180));
            _font.DrawString(_spriteBatch, "DEBUG", start, Color.Orange, 1.8f);
            _font.DrawString(_spriteBatch, $"Customers: {_customers.Count}", start + new Vector2(0, 16), Color.LightGray, 1.2f);
            _font.DrawString(_spriteBatch, $"Avg Sat: {avgSat:0.0}", start + new Vector2(0, 30), Color.LightGray, 1.2f);
            _font.DrawString(_spriteBatch, $"Staff B/D/J: {bouncers}/{bartenders}/{djs}", start + new Vector2(0, 44), Color.LightGray, 1.2f);
            _font.DrawString(_spriteBatch, $"Rating: {_clubRating:0.00}", start + new Vector2(0, 58), Color.LightGray, 1.2f);
        }

        private void DrawVignette()
        {
            var width = _graphics.PreferredBackBufferWidth;
            var height = _graphics.PreferredBackBufferHeight;
            int border = 40;
            _spriteBatch.Draw(_panelTexture, new Rectangle(0, 0, width, border), new Color(0, 0, 0, 60));
            _spriteBatch.Draw(_panelTexture, new Rectangle(0, height - border, width, border), new Color(0, 0, 0, 80));
            _spriteBatch.Draw(_panelTexture, new Rectangle(0, 0, border, height), new Color(0, 0, 0, 60));
            _spriteBatch.Draw(_panelTexture, new Rectangle(width - border, 0, border, height), new Color(0, 0, 0, 80));
        }

        private void DrawShadow(Vector2 pos)
        {
            _spriteBatch.Draw(_panelTexture, new Rectangle((int)pos.X - 8, (int)pos.Y, 16, 6), new Color(0, 0, 0, 100));
        }

        private void AddLog(string message)
        {
            _log.Add(message);
            if (_log.Count > 8) _log.RemoveAt(0);
        }
    }
}
