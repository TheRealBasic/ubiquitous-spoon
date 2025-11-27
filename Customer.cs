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

    public class Customer : Entity
    {
        public CustomerState State = CustomerState.Arriving;
        private float _useTimer = 0f;
        private readonly Random _random;
        private float _patience;
        public bool FrustratedLeave { get; private set; }

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
                        }
                        else
                        {
                            PickRandomTarget(world);
                        }
                    }
                    break;
                case CustomerState.Using:
                    _useTimer -= dt;
                    if (_useTimer <= 0f)
                    {
                        State = CustomerState.Wandering;
                        PickRandomTarget(world);
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
                }
            }
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
    }
}
