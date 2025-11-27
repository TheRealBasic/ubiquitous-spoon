using Microsoft.Xna.Framework;

namespace NightclubSim
{
    public abstract class Entity
    {
        public Vector2 GridPosition;
        public Vector2 TargetPosition;
        public float MoveTimer;
        public float MoveInterval = 0.3f;
        public Vector2 PreviousPosition;
        public Vector2 SmoothPosition;
        public float LerpTimer;

        public virtual void Update(GameTime gameTime, World world)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            MoveTimer -= dt;
            LerpTimer += dt;
            if (MoveTimer <= 0f)
            {
                PreviousPosition = GridPosition;
                StepTowardsTarget(world);
                MoveTimer = MoveInterval;
                LerpTimer = 0f;
            }

            float progress = MoveInterval <= 0f ? 1f : MathHelper.Clamp(LerpTimer / MoveInterval, 0f, 1f);
            SmoothPosition = Vector2.Lerp(PreviousPosition, GridPosition, progress);
        }

        protected void StepTowardsTarget(World world)
        {
            var current = new Point((int)GridPosition.X, (int)GridPosition.Y);
            var target = new Point((int)TargetPosition.X, (int)TargetPosition.Y);
            var next = current;
            if (current != target)
            {
                if (current.X < target.X) next.X++;
                else if (current.X > target.X) next.X--;
                if (current.Y < target.Y) next.Y++;
                else if (current.Y > target.Y) next.Y--;
            }
            if (world.IsInside(next.X, next.Y) && world.GetTile(next.X, next.Y).Walkable)
            {
                GridPosition = new Vector2(next.X, next.Y);
            }
        }
    }
}
