using System;
using System.Collections.Generic;
using System.Linq;
using static UnifiedExperienceSystem.ModEntry;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace UnifiedExperienceSystem
{
    public class UnifiedExperienceAPI : IUnifiedExperienceAPI
    {
        private readonly ModEntry mod;

        public UnifiedExperienceAPI(ModEntry mod)
        {
            this.mod = mod;
        }




        // =========================================================
        //  Start-of-day EXP and Level
        // =========================================================

        public int GetStartOfDayExp(string skillName) =>
            mod.startOfDayExp.TryGetValue(skillName, out int xp) ? xp : 0;

        public void SetStartOfDayExp(string skillName, int xp) =>
            mod.startOfDayExp[skillName] = xp;


        public IDictionary<string, int> GetAllStartOfDayExp() =>
            new Dictionary<string, int>(mod.startOfDayExp);

        public void SetAllStartOfDayExp(IDictionary<string, int> dict)
        {
            mod.startOfDayExp.Clear();
            foreach (var pair in dict)
                mod.startOfDayExp[pair.Key] = pair.Value;
        }

        public int GetStartOfDayLevel(string skillName) =>
            mod.startOfDayLevel.TryGetValue(skillName, out int level) ? level : 0;

        public void SetStartOfDayLevel(string skillName, int level) =>
            mod.startOfDayLevel[skillName] = level;

        public IDictionary<string, int> GetAllStartOfDayLevel() =>
            new Dictionary<string, int>(mod.startOfDayLevel);

        public void SetAllStartOfDayLevel(IDictionary<string, int> dict)
        {
            mod.startOfDayLevel.Clear();
            foreach (var pair in dict)
                mod.startOfDayLevel[pair.Key] = pair.Value;
        }

        // =========================================================
        //  Global EXP System
        // =========================================================

        public int GetGlobalEXP() => mod.SaveData.GlobalEXP;
        public void SetGlobalEXP(int value) => mod.SaveData.GlobalEXP = value;

        public int GetUnspentSkillPoints() => mod.SaveData.UnspentSkillPoints;
        public void SetUnspentSkillPoints(int value) => mod.SaveData.UnspentSkillPoints = value;

        public IEnumerable<string> GetAllSkillNames()
        {
            var names = new List<string>();
            foreach (var skill in mod.LoadAllSkills())
                names.Add(skill.DisplayName);
            return names;
        }

        // =========================================================
        //  Abilities
        //  - Register runtime ability defs
        //  - Compute level/progress from persisted totals
        // =========================================================

        private readonly Dictionary<(string modId, string abilityId), AbilityDef> _abilities
            = new(new ModAbilityKeyComparer());


        public IEnumerable<(string modId, string abilityId, string displayName, string description, int maxLevel)> ListRegisteredAbilities()
    => _abilities.Values.Select(a => (a.ModId, a.AbilityId, a.DisplayName, a.Description, a.MaxLevel));


        private sealed class AbilityDef
        {
            public string ModId = "";
            public string AbilityId = "";
            public string DisplayName = "";
            public string Description = "";
            public string CurveKind = "linear";
            public int LinearXpPerLevel = 100;
            public int StepBase = 100;
            public int StepPerLevel = 0;
            public int[] TableLevels = Array.Empty<int>();
            public long[]? TablePrefix;

            public int MaxLevel = 1;
            public string? IconPath;
            public string[]? Tags;
        }

        // ------------------------------------------------------------------------------------
        // OLD OVERLOAD (kept for compatibility) → forwards to the new overload/core
        // ------------------------------------------------------------------------------------
        public void RegisterAbility(
            string modUniqueId,
            string abilityId,
            string displayName,
            string description,
            string curveKind,
            IDictionary<string, object> curveData,
            int maxLevel,
            string? iconPath = null,
            string[]? tags = null
        )
        {
            RegisterAbilityCore(
                modUniqueId,
                abilityId,
                displayName,
                description,
                curveKind,
                curveData,
                maxLevel,
                iconTexture: null,
                iconSourceRect: null,
                iconPath: iconPath,
                tags: tags
            );
        }


        // ------------------------------------------------------------------------------------
        // NEW OVERLOAD (matches IUnifiedExperienceAPI exactly)
        // ------------------------------------------------------------------------------------
        public void RegisterAbility(
            string modUniqueId,
            string abilityId,
            string displayName,
            string description,
            string curveKind,
            IDictionary<string, object> curveData,
            int maxLevel,
            Texture2D? iconTexture,
            Rectangle? iconSourceRect = null,
            string? iconPath = null,
            string[]? tags = null
        )
        {
            RegisterAbilityCore(
                modUniqueId,
                abilityId,
                displayName,
                description,
                curveKind,
                curveData,
                maxLevel,
                iconTexture,
                iconSourceRect,
                iconPath,
                tags
            );
        }



        // ------------------------------------------------------------------------------------
        // CORE IMPLEMENTATION (single place that does all the work)
        // ------------------------------------------------------------------------------------
        private void RegisterAbilityCore(
            string modUniqueId,
            string abilityId,
            string displayName,
            string description,
            string curveKind,
            IDictionary<string, object> curveData,
            int maxLevel,
            Texture2D? iconTexture,
            Rectangle? iconSourceRect,
            string? iconPath,
            string[]? tags
        )
        {
            if (string.IsNullOrWhiteSpace(modUniqueId)) throw new ArgumentException("modUniqueId required");
            if (string.IsNullOrWhiteSpace(abilityId)) throw new ArgumentException("abilityId required");
            if (maxLevel < 1) throw new ArgumentException("maxLevel must be >= 1");

            // Normalize tags: trim, drop empties, distinct (case-insensitive)
            static string[] NormalizeTags(string[]? input)
                => input == null
                    ? Array.Empty<string>()
                    : input
                        .Select(t => t?.Trim())
                        .Where(t => !string.IsNullOrEmpty(t))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();

            var def = new AbilityDef
            {
                ModId = modUniqueId,
                AbilityId = abilityId,
                DisplayName = displayName ?? "",
                Description = description ?? "",
                CurveKind = (curveKind ?? "linear").ToLowerInvariant(),
                MaxLevel = maxLevel,

                // Store metadata for renderer/fallback logic
                IconPath = string.IsNullOrWhiteSpace(iconPath) ? null : iconPath,
                Tags = NormalizeTags(tags)
            };

            // NOTE: If you later add fields to AbilityDef for resolved icons, set them here, e.g.:
            // def.IconTexture = iconTexture;
            // def.IconSourceRect = iconTexture != null
            //     ? (iconSourceRect ?? new Rectangle(0, 0, iconTexture.Width, iconTexture.Height))
            //     : (Rectangle?)null;

            switch (def.CurveKind)
            {
                case "linear":
                    def.LinearXpPerLevel = GetInt(curveData, "xpPerLevel", min: 1);
                    break;

                case "step":
                    def.StepBase = GetInt(curveData, "base", min: 0);
                    def.StepPerLevel = GetInt(curveData, "step", min: 0);
                    break;

                case "table":
                    {
                        var levels = GetIntArray(curveData, "levels");
                        if (levels.Length == 0 || levels.Any(v => v <= 0))
                            throw new ArgumentException("table 'levels' must be positive and non-empty.");

                        def.TableLevels = levels;
                        var cap = Math.Min(def.MaxLevel, def.TableLevels.Length);
                        def.TablePrefix = new long[cap + 1];
                        for (int i = 1; i <= cap; i++)
                            def.TablePrefix[i] = def.TablePrefix[i - 1] + def.TableLevels[i - 1];
                        break;
                    }

                default:
                    def.CurveKind = "linear";
                    def.LinearXpPerLevel = 100;
                    break;
            }

            _abilities[(modUniqueId, abilityId)] = def;

            // ---- local helpers (same as before) ----
            static int GetInt(IDictionary<string, object> data, string key, int min)
            {
                if (!data.TryGetValue(key, out var v)) throw new ArgumentException($"missing '{key}'");
                int val = v switch
                {
                    int i => i,
                    long l => checked((int)l),
                    string s when int.TryParse(s, out var p) => p,
                    _ => throw new ArgumentException($"invalid '{key}'")
                };
                if (val < min) throw new ArgumentException($"'{key}' must be >= {min}");
                return val;
            }

            static int[] GetIntArray(IDictionary<string, object> data, string key)
            {
                if (!data.TryGetValue(key, out var v)) throw new ArgumentException($"missing '{key}'");

                if (v is int[] arr) return arr;
                if (v is IEnumerable<object> e)
                {
                    var list = new List<int>();
                    foreach (var item in e)
                    {
                        int val = item switch
                        {
                            int i => i,
                            long l => checked((int)l),
                            string s when int.TryParse(s, out var p) => p,
                            _ => throw new ArgumentException($"invalid element in '{key}'")
                        };
                        list.Add(val);
                    }
                    return list.ToArray();
                }
                throw new ArgumentException($"'{key}' must be int[]");
            }
        }

        public int GetAbilityLevel(string modUniqueId, string abilityId)
        {
            var key = (modUniqueId, abilityId);
            if (!_abilities.TryGetValue(key, out var def)) return 0;

            long exp = GetTotalExpPersisted(modUniqueId, abilityId);
            var (level, _, _, _) = ComputeProgress(def, exp);
            return level;
        }

        public void GrantAbilityExp(string modUniqueId, string abilityId, int exp)
        {
            if (exp <= 0) return;


            var list = mod.SaveData.Abilities ??= new List<AbilitySaveData>();


            var entry = list.FirstOrDefault(a =>
                string.Equals(a.ModGuid, modUniqueId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(a.AbilityId, abilityId, StringComparison.OrdinalIgnoreCase));


            if (entry == null)
            {
                entry = new AbilitySaveData
                {
                    ModGuid = modUniqueId,
                    AbilityId = abilityId,
                    TotalExpSpent = 0
                };
                list.Add(entry);
            }


            checked
            {
                entry.TotalExpSpent = Math.Max(0, entry.TotalExpSpent + exp);
            }
        }



        public (int expInto, int expNeeded, int maxLevel) GetAbilityProgress(string modUniqueId, string abilityId)
        {
            var key = (modUniqueId, abilityId);
            if (!_abilities.TryGetValue(key, out var def)) return (0, 0, 1);

            long exp = GetTotalExpPersisted(modUniqueId, abilityId);
            var (_, into, nextLevelCost, atMax) = ComputeProgress(def, exp);
            int neededRemaining = atMax ? 0 : Math.Max(0, nextLevelCost - into);
            return (into, neededRemaining, def.MaxLevel);
        }

        public bool IsAbilityAtMax(string modUniqueId, string abilityId)
        {
            var key = (modUniqueId, abilityId);
            if (!_abilities.TryGetValue(key, out var def)) return false;

            long exp = GetTotalExpPersisted(modUniqueId, abilityId);
            var (_, _, _, atMax) = ComputeProgress(def, exp);
            return atMax;
        }

        public int GetAbilityRemainingXpToCap(string modId, string abilityId)
        {
            if (!_abilities.TryGetValue((modId, abilityId), out var def)) return int.MaxValue;
            long exp = GetTotalExpPersisted(modId, abilityId);
            var (level, into, nextCost, atMax) = ComputeProgress(def, exp);
            if (atMax) return 0;

            long remain = Math.Max(0, nextCost - into);
            for (int L = level + 2; L <= def.MaxLevel; L++)
                remain += CostForLevel(def, L);

            return remain > int.MaxValue ? int.MaxValue : (int)remain;
        }



        private static int CostForLevel(AbilityDef def, int level)
        {
            if (level <= 0) level = 1;

            switch (def.CurveKind)
            {
                case "linear":

                    return Math.Max(1, def.LinearXpPerLevel);

                case "step":

                    return Math.Max(0, def.StepBase + def.StepPerLevel * (level - 1));

                case "table":
                    {
                        var arr = def.TableLevels ?? Array.Empty<int>();
                        if (arr.Length == 0) return 1;
                        int idx = level - 1;
                        if (idx < 0) idx = 0;
                        if (idx >= arr.Length) idx = arr.Length - 1;
                        return Math.Max(1, arr[idx]);
                    }

                default:
                    return 1;
            }
        }

        public string GetAbilityDisplayName(string modUniqueId, string abilityId)
        => _abilities.TryGetValue((modUniqueId, abilityId), out var def)
        ? def.DisplayName : "";


        // ---------------------------------------------------------
        // Helpers: read persisted EXP and compute curve progress
        // ---------------------------------------------------------

        private long GetTotalExpPersisted(string modId, string abilityId)
        {

            var list = mod.SaveData.Abilities;
            if (list == null || list.Count == 0) return 0;

            for (int i = 0; i < list.Count; i++)
            {
                var a = list[i];
                if (a == null) continue;
                if (string.Equals(a.ModGuid, modId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(a.AbilityId, abilityId, StringComparison.OrdinalIgnoreCase))
                {
                    return a.TotalExpSpent < 0 ? 0 : a.TotalExpSpent;
                }
            }
            return 0;
        }

        private static (int level, int into, int needed, bool atMax) ComputeProgress(AbilityDef def, long exp)
        {
            exp = Math.Max(0, exp);
            int cap = Math.Max(1, def.MaxLevel);

            switch (def.CurveKind)
            {
                case "linear":
                    {
                        int c = Math.Max(1, def.LinearXpPerLevel);
                        int L = (int)Math.Min(cap, exp / c);
                        bool maxed = L >= cap;
                        int into = maxed ? 0 : (int)(exp % c);
                        int needed = maxed ? 0 : c;
                        return (L, into, needed, maxed);
                    }

                case "step":
                    {

                        static long S(long L, long b, long d) => L * (2 * b + (L - 1) * d) / 2;
                        long b = Math.Max(0, def.StepBase);
                        long d = Math.Max(0, def.StepPerLevel);

                        int lo = 0, hi = cap;
                        while (lo < hi)
                        {
                            int mid = (lo + hi + 1) >> 1;
                            if (S(mid, b, d) <= exp) lo = mid; else hi = mid - 1;
                        }
                        int L = lo;
                        bool maxed = L >= cap;
                        long spent = S(L, b, d);
                        int into = (int)Math.Max(0, exp - spent);
                        int needed = maxed ? 0 : (int)(b + (long)L * d);
                        return (L, into, needed, maxed);
                    }

                case "table":
                    {
                        var levels = def.TableLevels ?? Array.Empty<int>();
                        int capEff = Math.Min(cap, levels.Length);
                        var pref = def.TablePrefix ?? Array.Empty<long>();
                        if (capEff <= 0) return (0, 0, 1, false);

                        int lo = 0, hi = capEff;
                        while (lo < hi)
                        {
                            int mid = (lo + hi + 1) >> 1;
                            if (pref[mid] <= exp) lo = mid; else hi = mid - 1;
                        }
                        int L = lo;
                        bool maxed = L >= capEff;
                        int into = (int)Math.Max(0, exp - pref[L]);
                        int needed = maxed ? 0 : levels[L];
                        return (L, into, needed, maxed);
                    }

                default:

                    const int cdef = 100;
                    int Ld = (int)Math.Min(cap, exp / cdef);
                    bool m = Ld >= cap;
                    int intod = m ? 0 : (int)(exp % cdef);
                    int needd = m ? 0 : cdef;
                    return (Ld, intod, needd, m);
            }
        }


        private sealed class ModAbilityKeyComparer : IEqualityComparer<(string modId, string abilityId)>
        {
            public bool Equals((string modId, string abilityId) x, (string modId, string abilityId) y)
                => string.Equals(x.modId, y.modId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.abilityId, y.abilityId, StringComparison.OrdinalIgnoreCase);

            public int GetHashCode((string modId, string abilityId) key)
            {
                unchecked
                {
                    int h1 = StringComparer.OrdinalIgnoreCase.GetHashCode(key.modId ?? string.Empty);
                    int h2 = StringComparer.OrdinalIgnoreCase.GetHashCode(key.abilityId ?? string.Empty);
                    return (h1 * 397) ^ h2;
                }
            }
        }

        // ---------------------------------------------------------
        // Energy
        // ---------------------------------------------------------
        public bool TryToUseAbility(float energyCost) => mod.TryToUseAbility(energyCost);
        public float GetCurrentEnergy() => mod.GetCurrentEnergyInternal();





    }

}