using System;
using Microsoft.Xna.Framework;

namespace NightclubSim
{
    public enum CustomerState
    {
        Arriving,
        Wandering,
        Using,
        Leaving
    }

    public enum CustomerPreference
    {
        Neutral,
        Drinks,
        Dance
    }

    public class Customer : Entity
    {
        public CustomerState State = CustomerState.Arriving;
        private float _useTimer = 0f;
        private readonly Random _random;
        private float _patience;
        public bool FrustratedLeave { get; private set; }

        public float Satisfaction { get; set; } = 60f;
        public CustomerPreference PreferredActivity { get; }
        public string CurrentActivity { get; private set; } = "Arriving";
        public bool IsSelected { get; set; }
        private float _thoughtTimer;

        public Customer(Random rng, Point spawn)
        {
            _random = rng;
            GridPosition = new Vector2(spawn.X, spawn.Y);
            TargetPosition = GridPosition + new Vector2(0, -1);
            PreviousPosition = GridPosition;
            SmoothPosition = GridPosition;
            MoveInterval = 0.45f;
            _patience = 30f + (float)_random.NextDouble() * 10f;
            FrustratedLeave = false;
            PreferredActivity = (CustomerPreference)_random.Next(0, 3);
            _thoughtTimer = 2.5f + (float)_random.NextDouble() * 3f;
        }

        public override void Update(GameTime gameTime, World world)
        {
            base.Update(gameTime, world);
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            switch (State)
            {
                case CustomerState.Arriving:
                    if (new Point((int)GridPosition.X, (int)GridPosition.Y) == new Point((int)TargetPosition.X, (int)TargetPosition.Y))
                    {
                        State = CustomerState.Wandering;
                        PickRandomTarget(world);
                        CurrentActivity = "Looking around";
                    }
                    break;
                case CustomerState.Wandering:
                    if (ReachedTarget())
                    {
                        var tile = world.GetTile((int)GridPosition.X, (int)GridPosition.Y);
                        if (tile.Type == TileType.Bar || tile.Type == TileType.DanceFloor || tile.Type == TileType.Table)
                        {
                            State = CustomerState.Using;
                            _useTimer = 3f + (float)_random.NextDouble() * 5f;
                            _patience = 35f + (float)_random.NextDouble() * 10f;
                            FrustratedLeave = false;
                            CurrentActivity = tile.Type switch
                            {
                                TileType.Bar => "At Bar",
                                TileType.DanceFloor => "On Dance Floor",
                                TileType.Table => "Sitting",
                                _ => "Wandering"
                            };
                        }
                        else
                        {
                            PickRandomTarget(world);
                            CurrentActivity = "Looking for Seat";
                        }
                    }
                    break;
                case CustomerState.Using:
                    _useTimer -= dt;
                    if (_useTimer <= 0f)
                    {
                        State = CustomerState.Wandering;
                        PickRandomTarget(world);
                        CurrentActivity = "Moving";
                    }
                    break;
                case CustomerState.Leaving:
                    if (ReachedTarget())
                    {
                        // Disappear handled externally.
                    }
                    break;
            }

            if (State == CustomerState.Wandering)
            {
                _patience -= dt;
                if (_patience <= 0f)
                {
                    State = CustomerState.Leaving;
                    TargetPosition = new Vector2(world.Entrance.X, world.Entrance.Y);
                    FrustratedLeave = true;
                    CurrentActivity = "Leaving";
                }
            }

            UpdateSatisfaction(dt, world);
            _thoughtTimer -= dt;
        }

        public bool ReachedTarget()
        {
            return (int)GridPosition.X == (int)TargetPosition.X && (int)GridPosition.Y == (int)TargetPosition.Y;
        }

        public void ForceLeave(World world)
        {
            State = CustomerState.Leaving;
            TargetPosition = new Vector2(world.Entrance.X, world.Entrance.Y);
            FrustratedLeave = false;
        }

        private void PickRandomTarget(World world)
        {
            for (int i = 0; i < 10; i++)
            {
                int x = _random.Next(1, world.Width - 1);
                int y = _random.Next(1, world.Height - 1);
                if (world.GetTile(x, y).Walkable)
                {
                    TargetPosition = new Vector2(x, y);
                    return;
                }
            }
            TargetPosition = GridPosition;
        }

        private void UpdateSatisfaction(float dt, World world)
        {
            float delta = -5f * dt; // boredom baseline
            var tile = world.GetTile((int)GridPosition.X, (int)GridPosition.Y);
            if (tile.Type == TileType.Bar)
            {
                delta += 9f * dt;
                if (PreferredActivity == CustomerPreference.Drinks) delta += 5f * dt;
            }
            else if (tile.Type == TileType.DanceFloor)
            {
                delta += 10f * dt;
                if (PreferredActivity == CustomerPreference.Dance) delta += 6f * dt;
            }
            else if (tile.Type == TileType.Table)
            {
                delta += 3f * dt;
            }

            // Nearby decor bonus
            int decorNearby = 0;
            foreach (var offset in new[] { new Point(1, 0), new Point(-1, 0), new Point(0, 1), new Point(0, -1) })
            {
                int nx = (int)GridPosition.X + offset.X;
                int ny = (int)GridPosition.Y + offset.Y;
                if (world.IsInside(nx, ny))
                {
                    var t = world.GetTile(nx, ny);
                    if (t.Type == TileType.Decor) decorNearby++;
                }
            }
            delta += decorNearby * 1.5f * dt;

            Satisfaction = MathHelper.Clamp(Satisfaction + delta, 0f, 100f);

            if (Satisfaction <= 5f && State != CustomerState.Leaving)
            {
                State = CustomerState.Leaving;
                TargetPosition = new Vector2(world.Entrance.X, world.Entrance.Y);
                FrustratedLeave = true;
                CurrentActivity = "Leaving";
            }
        }

        public string GetMoodDescription()
        {
            if (Satisfaction >= 80f) return "Loving it!";
            if (Satisfaction >= 60f) return PreferredActivity == CustomerPreference.Dance ? "Enjoying the beats" : "Feeling good";
            if (Satisfaction >= 40f) return "Wants a drink";
            if (Satisfaction >= 20f) return "Bored...";
            return "About to leave";
        }

        public bool ShouldShowThought() => _thoughtTimer <= 0f;

        public void ResetThoughtTimer()
        {
            _thoughtTimer = 2.5f + (float)_random.NextDouble() * 4f;
        }
    }
}
