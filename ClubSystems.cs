using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace NightclubSim
{
    public enum WeatherType
    {
        Clear,
        Rain,
        Heatwave,
        Storm
    }

    public enum StaffTrait
    {
        Charismatic,
        Meticulous,
        FastLearner,
        HeavyHitter,
        Calm
    }

    public enum AchievementType
    {
        FirstNight,
        PackedHouse,
        CleanClub,
        HighReputation,
        PowerSurvivor,
        ResearchUnlocked
    }

    public record SupplierContract(string Name, float CostModifier, float QualityModifier, float Reliability);
    public record MarketingCampaign(string Channel, float AttendanceBoost, float SentimentBoost, float Duration);

    public class ResearchNode
    {
        public string Id { get; }
        public string Description { get; }
        public int Cost { get; }
        public bool Unlocked { get; private set; }

        public ResearchNode(string id, string description, int cost)
        {
            Id = id;
            Description = description;
            Cost = cost;
        }

        public bool TryUnlock(ref int points)
        {
            if (Unlocked || points < Cost) return false;
            points -= Cost;
            Unlocked = true;
            return true;
        }
    }

    public class ClubSystems
    {
        public bool SandboxMode { get; set; }
        public bool TicketingEnabled { get; set; } = true;
        public float TicketPrice { get; set; } = 10f;
        public float Reputation { get; private set; } = 1f;
        public float CloudSyncToggle { get; set; } = 0f;

        public WeatherType Weather { get; private set; } = WeatherType.Clear;
        private float _weatherTimer = 45f;

        private readonly List<SupplierContract> _contracts = new();
        private SupplierContract? _activeContract;
        private int _inventory = 80;
        private float _restockTimer;

        private readonly List<MarketingCampaign> _campaigns = new();
        private float _campaignTimer;

        public string TonightTheme { get; private set; } = "None";
        private float _themeTimer = 60f;

        public bool RopeQueueEnabled { get; set; } = true;
        public int DeniedGuests { get; private set; }
        public float QueueFairnessBonus { get; private set; }

        public int ResearchPoints { get; private set; } = 5;
        public IReadOnlyList<ResearchNode> ResearchNodes => _research;
        private readonly List<ResearchNode> _research = new();

        public float Cleanliness { get; private set; } = 1f;
        private float _trash;
        public float Safety { get; private set; } = 0.75f;
        private float _powerBuffer = 1f;
        public float PowerLoad { get; private set; }
        private float _outageTimer;

        public int Floors { get; private set; } = 1;
        public float FacadeAppeal { get; private set; } = 0.2f;
        public string Branding { get; private set; } = "Chrome Pulse";

        public HashSet<AchievementType> Achievements { get; } = new();

        public float StaffFatigue { get; private set; }
        public float StaffTraining { get; private set; }

        public float DanceHype { get; private set; } = 0.5f;
        private float _playlistBpm = 120f;

        public int NightlyIncidents { get; private set; }
        public int NightlyEmails { get; private set; }

        private float _summaryTimer;

        public bool ShowPerformanceOverlay { get; set; }
        public bool ShowTooltipOverlay { get; set; } = true;

        public ClubSystems()
        {
            _research.Add(new ResearchNode("decor", "Unlocks premium decor", 3));
            _research.Add(new ResearchNode("safety", "Fire safety drills", 4));
            _research.Add(new ResearchNode("marketing", "Better social ads", 3));

            _contracts.Add(new SupplierContract("Budget Brewer", 0.85f, 0.8f, 0.9f));
            _contracts.Add(new SupplierContract("Premium Spirits", 1.15f, 1.1f, 0.75f));
            _contracts.Add(new SupplierContract("Local Craft", 1.05f, 1.15f, 0.8f));
            _activeContract = _contracts[0];
        }

        public void ToggleSandbox(Economy economy)
        {
            SandboxMode = !SandboxMode;
            economy.Sandbox = SandboxMode;
        }

        public void Update(Economy economy, World world, List<Customer> customers, List<Staff> staff, Random rng, Action<string> log, float dt)
        {
            UpdateWeather(rng, log, dt);
            UpdateTheme(rng, log, dt);
            UpdateMarketing(log, dt);
            UpdateInventory(rng, log, dt);
            UpdateCleanliness(staff, rng, log, dt);
            UpdateSafety(rng, log, dt);
            UpdatePower(rng, log, dt);
            UpdateStaff(staff, dt);
            UpdatePlaylist(dt);
            UpdateReputation(customers, dt);
            HandleAchievements(log, customers);
            UpdateSummary(log, economy, customers, dt);
            ApplyTicketIncome(economy, customers, dt);
            ApplyQueueBonuses(customers, dt);
            ApplyVIPBonuses(customers, economy);
            UpdateIncidents(rng, log, dt);
        }

        private void UpdateWeather(Random rng, Action<string> log, float dt)
        {
            _weatherTimer -= dt;
            if (_weatherTimer <= 0f)
            {
                Weather = (WeatherType)rng.Next(0, 4);
                _weatherTimer = 60f + (float)rng.NextDouble() * 45f;
                log($"Weather shifted to {Weather}.");
            }
        }

        private void UpdateTheme(Random rng, Action<string> log, float dt)
        {
            _themeTimer -= dt;
            if (_themeTimer <= 0f)
            {
                var themes = new[] { "Retro Wave", "VIP Lounge", "Techno Marathon", "Silent Disco" };
                TonightTheme = themes[rng.Next(themes.Length)];
                _themeTimer = 120f;
                log($"Tonight's theme is {TonightTheme}!");
            }
        }

        private void UpdateMarketing(Action<string> log, float dt)
        {
            if (_campaignTimer <= 0f && _campaigns.Count < 2)
            {
                _campaigns.Add(new MarketingCampaign("Social Ads", 0.2f, 0.05f, 45f));
                log("Launched a social ad campaign.");
                _campaignTimer = 30f;
            }
            else
            {
                _campaignTimer -= dt;
            }

            for (int i = _campaigns.Count - 1; i >= 0; i--)
            {
                var campaign = _campaigns[i];
                var remaining = campaign.Duration - dt;
                _campaigns[i] = campaign with { Duration = remaining };
                if (remaining <= 0)
                {
                    log($"{campaign.Channel} campaign ended.");
                    _campaigns.RemoveAt(i);
                }
                else
                {
                    Reputation += campaign.SentimentBoost * dt;
                }
            }
        }

        private void UpdateInventory(Random rng, Action<string> log, float dt)
        {
            _inventory -= (int)(dt * 2);
            if (_inventory <= 15 && _restockTimer <= 0f)
            {
                _restockTimer = 10f;
                var failChance = 1f - (_activeContract?.Reliability ?? 1f);
                if (rng.NextDouble() > failChance)
                {
                    int restock = (int)(50 * (_activeContract?.QualityModifier ?? 1f));
                    _inventory += restock;
                    log($"Restocked {restock} drinks from {_activeContract?.Name}.");
                }
                else
                {
                    log($"{_activeContract?.Name} delivery delayed.");
                }
            }
            if (_restockTimer > 0f) _restockTimer -= dt;
        }

        private void UpdateCleanliness(List<Staff> staff, Random rng, Action<string> log, float dt)
        {
            _trash += dt * 0.5f;
            Cleanliness = MathHelper.Clamp(Cleanliness - dt * 0.01f - _trash * 0.0005f, 0f, 1f);
            foreach (var _ in staff.Where(s => s.Role == StaffRole.Bartender || s.Role == StaffRole.Bouncer))
            {
                if (_trash > 0f)
                {
                    _trash = Math.Max(0f, _trash - dt * 3f);
                    Cleanliness = MathHelper.Clamp(Cleanliness + dt * 0.02f, 0f, 1f);
                }
            }
            if (Cleanliness < 0.35f && rng.NextDouble() < 0.01f)
            {
                log("Guests complain about dirty restrooms.");
            }
        }

        private void UpdateSafety(Random rng, Action<string> log, float dt)
        {
            Safety = MathHelper.Clamp(Safety - dt * 0.005f, 0f, 1f);
            if (Safety < 0.25f && rng.NextDouble() < 0.005f)
            {
                NightlyIncidents++;
                Safety += 0.1f;
                log("Triggered a fire drill to reset safety lapses.");
            }
        }

        private void UpdatePower(Random rng, Action<string> log, float dt)
        {
            PowerLoad = MathHelper.Clamp(PowerLoad + dt * 0.02f, 0f, 2f);
            _powerBuffer = MathHelper.Clamp(_powerBuffer - PowerLoad * dt * 0.05f, 0f, 1f);
            if (_powerBuffer <= 0f)
            {
                _outageTimer += dt;
                if (_outageTimer > 3f)
                {
                    log("Backup generators kicked in after an outage.");
                    _powerBuffer = 0.5f;
                    _outageTimer = 0f;
                }
            }
            else
            {
                _outageTimer = Math.Max(0f, _outageTimer - dt);
            }
        }

        private void UpdateStaff(List<Staff> staff, float dt)
        {
            StaffFatigue = MathHelper.Clamp(StaffFatigue + dt * 0.01f, 0f, 1f);
            StaffTraining = MathHelper.Clamp(StaffTraining + dt * 0.02f, 0f, 1.5f);
            foreach (var member in staff)
            {
                if (member.Trait == null)
                {
                    member.Trait = (StaffTrait)(member.Role.GetHashCode() % Enum.GetValues(typeof(StaffTrait)).Length);
                }
            }
        }

        private void UpdatePlaylist(float dt)
        {
            _playlistBpm = MathHelper.Clamp(_playlistBpm + (float)Math.Sin(DateTime.Now.TimeOfDay.TotalSeconds) * dt * 10f, 100f, 140f);
            DanceHype = MathHelper.Clamp(DanceHype + (_playlistBpm - 120f) * 0.0005f, 0.1f, 1.5f);
        }

        private void UpdateReputation(List<Customer> customers, float dt)
        {
            float mood = customers.Count == 0 ? 0.5f : customers.Average(c => c.Satisfaction) / 100f;
            Reputation = MathHelper.Clamp(Reputation + (mood - 0.5f) * dt * 0.2f, 0.2f, 5f);
        }

        private void HandleAchievements(Action<string> log, List<Customer> customers)
        {
            if (customers.Count > 30 && Achievements.Add(AchievementType.PackedHouse)) log("Achievement unlocked: Packed House!");
            if (Cleanliness > 0.9f && Achievements.Add(AchievementType.CleanClub)) log("Achievement unlocked: Spotless!\n");
            if (Reputation > 3.5f && Achievements.Add(AchievementType.HighReputation)) log("Achievement unlocked: Talk of the Town!");
            if (_research.Any(r => r.Unlocked) && Achievements.Add(AchievementType.ResearchUnlocked)) log("Achievement unlocked: Innovator!");
        }

        private void UpdateSummary(Action<string> log, Economy economy, List<Customer> customers, float dt)
        {
            _summaryTimer += dt;
            if (_summaryTimer > 90f)
            {
                NightlyEmails++;
                log($"Daily summary: ${economy.Money} cash, {customers.Count} guests, rep {Reputation:F1}.");
                _summaryTimer = 0f;
            }
        }

        private void ApplyTicketIncome(Economy economy, List<Customer> customers, float dt)
        {
            if (!TicketingEnabled) return;
            float demand = MathHelper.Clamp(1f + (Reputation - 1f) * 0.1f - TicketPrice * 0.02f, 0.2f, 2f);
            var bonus = (int)(customers.Count * demand * dt);
            if (bonus > 0) economy.AddIncome(bonus);
        }

        private void ApplyQueueBonuses(List<Customer> customers, float dt)
        {
            if (!RopeQueueEnabled) return;
            QueueFairnessBonus = MathHelper.Clamp(QueueFairnessBonus + customers.Count * 0.0005f * dt, 0f, 0.5f);
            foreach (var c in customers)
            {
                c.Satisfaction = MathHelper.Clamp(c.Satisfaction + QueueFairnessBonus, 0f, 100f);
            }
        }

        private void ApplyVIPBonuses(List<Customer> customers, Economy economy)
        {
            foreach (var vip in customers.Where(c => c.IsVip))
            {
                economy.AddIncome(1);
                vip.Satisfaction = MathHelper.Clamp(vip.Satisfaction + 0.1f, 0f, 120f);
            }
        }

        private void UpdateIncidents(Random rng, Action<string> log, float dt)
        {
            if (rng.NextDouble() < dt * 0.05f)
            {
                NightlyIncidents++;
                log("Security contained a minor incident.");
            }
        }

        public float GetSpawnModifier()
        {
            float mod = 1f;
            mod += _campaigns.Sum(c => c.AttendanceBoost);
            mod += (Reputation - 1f) * 0.1f;
            mod += (Weather == WeatherType.Rain || Weather == WeatherType.Storm) ? -0.2f : 0.1f;
            mod += TonightTheme == "VIP Lounge" ? 0.2f : 0f;
            mod += FacadeAppeal * 0.1f;
            return MathHelper.Clamp(mod, 0.3f, 2.5f);
        }

        public void UnlockResearch(string id)
        {
            var node = _research.FirstOrDefault(r => r.Id == id);
            if (node != null && node.TryUnlock(ref ResearchPoints))
            {
                Reputation += 0.1f;
            }
        }

        public void UpgradeFacade()
        {
            FacadeAppeal = MathHelper.Clamp(FacadeAppeal + 0.1f, 0f, 2f);
        }

        public void AddFloor()
        {
            Floors = Math.Min(3, Floors + 1);
        }

        public void SwitchSupplier(string name)
        {
            _activeContract = _contracts.FirstOrDefault(c => c.Name == name) ?? _activeContract;
        }
    }
}
