using StardewModdingAPI;
using StardewModdingAPI.Events;

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

        // called when player spends points or mods add XP
        public void AllocateAbilityPoints(string modGuid, string abilityId, int expToAdd = -1)
        {
            if (SaveData.UnspentSkillPoints <= 0 && expToAdd <= 0)
                return;

            SaveData.Abilities ??= new List<AbilitySaveData>();

            var entry = SaveData.Abilities
                .FirstOrDefault(a => a.ModGuid == modGuid && a.AbilityId == abilityId);

            if (entry == null)
            {
                entry = new AbilitySaveData
                {
                    ModGuid = modGuid,
                    AbilityId = abilityId,
                    TotalExpSpent = 0
                };
                SaveData.Abilities.Add(entry);
            }

            // spend points if none supplied
            if (expToAdd <= 0)
            {
                int pointsToUse = Math.Min(Config.PointsAllocatedPerClick, SaveData.UnspentSkillPoints);
                expToAdd = EXP_PER_POINT * pointsToUse;
                SaveData.UnspentSkillPoints -= pointsToUse;
            }

            // --- MAX CAP LOGIC BASED ON CURVE ---
            int maxExp = GetMaxExpForAbility(modGuid, abilityId);
            int newExp = (int)(entry.TotalExpSpent + expToAdd);

            if (newExp > maxExp)
            {
                expToAdd = (int)(maxExp - entry.TotalExpSpent); // only add up to cap
                int refund = (newExp - maxExp) / EXP_PER_POINT;
                SaveData.UnspentSkillPoints += refund; // refund excess
            }

            entry.TotalExpSpent += expToAdd;

            if (Config.DebugMode)
                Monitor.Log(
                    $"[Abilities] Added {expToAdd} XP → {modGuid}/{abilityId}. " +
                    $"Total = {entry.TotalExpSpent}/{maxExp}. " +
                    $"Points left = {SaveData.UnspentSkillPoints}",
                    LogLevel.Debug
                );
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

    }
}
