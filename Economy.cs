using System;

namespace NightclubSim
{
    public class Economy
    {
        public int Money { get; private set; } = 500;
        public int Level { get; private set; } = 1;
        public int Experience { get; private set; } = 0;
        public int ExperienceToNext => 100 + (Level - 1) * 50;

        public event Action<string>? Log;
        public event Action<int>? LevelledUp;

        public bool Spend(int amount)
        {
            if (Money < amount) return false;
            Money -= amount;
            return true;
        }

        public void AddIncome(int amount)
        {
            Money += amount;
            GrantXp(amount);
        }

        public void GrantXp(int amount)
        {
            Experience += amount;
            while (Experience >= ExperienceToNext)
            {
                Experience -= ExperienceToNext;
                Level++;
                Log?.Invoke($"Leveled up to {Level}!");
                LevelledUp?.Invoke(Level);
            }
        }

        public void ChargeUpkeep(int amount)
        {
            Money -= amount;
            if (Money < 0)
            {
                Money = 0;
                Log?.Invoke("Upkeep drained funds!");
            }
        }

        public void LoadState(int money, int level, int xp)
        {
            Money = money;
            Level = Math.Max(1, level);
            Experience = Math.Max(0, xp);
        }
    }
}
