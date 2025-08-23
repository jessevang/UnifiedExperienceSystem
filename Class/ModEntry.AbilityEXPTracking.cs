using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace UnifiedExperienceSystem
{
    public partial class ModEntry
    {
        // In ModEntry
        private readonly Dictionary<(string modId, string abilityId), AbilityRegistration> abilityRegistry = new();

        public class AbilityRegistration
        {
            public string ModId { get; set; }
            public string AbilityId { get; set; }
            public string DisplayName { get; set; } = "";
            public string CurveKind { get; set; } = "";
            public Dictionary<string, object> CurveData { get; set; } = new();
            public int MaxLevel { get; set; }
        }


        public void AllocateAbilityPoints(string modGuid, string abilityId, int expToAdd = -1)
        {
            if (SaveData.UnspentSkillPoints <= 0 && expToAdd <= 0)
                return;

            SaveData.Abilities ??= new List<AbilitySaveData>();

            var entry = SaveData.Abilities.FirstOrDefault(a =>
                string.Equals(a.ModGuid, modGuid, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(a.AbilityId, abilityId, StringComparison.OrdinalIgnoreCase));

            if (entry == null)
            {
                entry = new AbilitySaveData { ModGuid = modGuid, AbilityId = abilityId, TotalExpSpent = 0 };
                SaveData.Abilities.Add(entry);
            }

            long current = Math.Max(0, (long)entry.TotalExpSpent);

            // ---------- How much XP is left before hard cap? ----------
            int oldLevel = uesApi?.GetAbilityLevel(modGuid, abilityId) ?? 0;
            int remainXpToCap = uesApi?.GetAbilityRemainingXpToCap(modGuid, abilityId) ?? int.MaxValue;
            if (remainXpToCap <= 0)
            {
                if (Config.DebugMode)
                    Monitor.Log($"[Abilities] {modGuid}/{abilityId} already at cap; ignoring spend.", LogLevel.Trace);
                return;
            }

            // ---------- Direct XP grant path ----------
            if (expToAdd > 0)
            {
                long applied = Math.Min(remainXpToCap, (long)expToAdd);
                entry.TotalExpSpent = checked((int)(current + applied));

                if (Config.DebugMode)
                    Monitor.Log($"[Abilities] +{applied} XP (external) → {entry.TotalExpSpent}/{GetMaxExpForAbility(modGuid, abilityId)} for {modGuid}/{abilityId}", LogLevel.Debug);
                return;
            }

            // ---------- Point-based spend path (player click) ----------
            int canSpend = Math.Min(Config.PointsAllocatedPerClick, SaveData.UnspentSkillPoints);
            if (canSpend <= 0)
                return;

            int pointsNeededToCap = (EXP_PER_POINT > 0)
                ? (int)Math.Ceiling(remainXpToCap / (double)EXP_PER_POINT)
                : canSpend;

            int pointsToApply = Math.Min(canSpend, pointsNeededToCap);

            long requestedXp = (long)pointsToApply * EXP_PER_POINT;
            long appliedXp = Math.Min(remainXpToCap, requestedXp);

            entry.TotalExpSpent = checked((int)(current + appliedXp));

            SaveData.UnspentSkillPoints -= pointsToApply;
            int newLevel = uesApi?.GetAbilityLevel(modGuid, abilityId) ?? oldLevel;
            if (newLevel > oldLevel)
                Game1.playSound("powerup");

            if (Config.DebugMode)
                Monitor.Log($"[Abilities] +{appliedXp} XP using {pointsToApply} pts → total {entry.TotalExpSpent}/{GetMaxExpForAbility(modGuid, abilityId)}", LogLevel.Debug);
        }








        private int GetMaxExpForAbility(string modId, string abilityId)
        {
            if (!abilityRegistry.TryGetValue((modId, abilityId), out var reg))
                return int.MaxValue;

            int maxLevel = reg.MaxLevel;
            if (maxLevel <= 0)
                return int.MaxValue;

            int totalXp = 0;

            switch (reg.CurveKind.ToLower())
            {
                case "linear":
                    int xpPerLevel = Convert.ToInt32(reg.CurveData["xpPerLevel"]);
                    totalXp = xpPerLevel * maxLevel;
                    break;

                case "step":
                    int baseXp = Convert.ToInt32(reg.CurveData["base"]);
                    int step = Convert.ToInt32(reg.CurveData["step"]);
                    for (int i = 1; i <= maxLevel; i++)
                        totalXp += baseXp + step * (i - 1);
                    break;

                case "table":
                    var levels = (reg.CurveData["levels"] as int[]) ?? Array.Empty<int>();
                    for (int i = 0; i < Math.Min(levels.Length, maxLevel); i++)
                        totalXp += levels[i];
                    break;
            }

            return totalXp;
        }

        //Used to add any necessary screens on level up when ability levels up, used generally to handle level pass level 10 when Vanilla code will no longer be used in this mod
        private void EnqueueVanillaLevelUps(int which, int fromLevel, int toLevel)
        {

            // respect your config; keep a sane upper bound
            int cap = Math.Clamp(Config.MaxSkillLevel, 10, 100);

            for (int L = fromLevel + 1; L <= toLevel && L <= cap; L++)
            {
                var entry = new Microsoft.Xna.Framework.Point(which, L);
                if (!Game1.player.newLevels.Contains(entry)) // avoid duplicates if multiple clicks
                    Game1.player.newLevels.Add(entry);
            }
        }



    }


}
