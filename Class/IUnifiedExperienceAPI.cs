using System;
using System.Collections.Generic;

namespace UnifiedExperienceSystem
{
    public interface IUnifiedExperienceAPI
    {
        // =========================================================
        //  Start-of-day EXP and Level
        // =========================================================
        int GetStartOfDayExp(string skillName);
        void SetStartOfDayExp(string skillName, int xp);
        IDictionary<string, int> GetAllStartOfDayExp();
        void SetAllStartOfDayExp(IDictionary<string, int> dict);

        int GetStartOfDayLevel(string skillName);
        void SetStartOfDayLevel(string skillName, int level);
        IDictionary<string, int> GetAllStartOfDayLevel();
        void SetAllStartOfDayLevel(IDictionary<string, int> dict);

        // =========================================================
        //  Global EXP System
        // =========================================================
        int GetGlobalEXP();
        void SetGlobalEXP(int value);

        int GetUnspentSkillPoints();
        void SetUnspentSkillPoints(int value);

        IEnumerable<string> GetAllSkillNames();

        // =========================================================
        //  Abilities
        // =========================================================
        IEnumerable<(string modId, string abilityId, string displayName, string Description, int maxLevel)> ListRegisteredAbilities();

        /// <summary>
        /// Register (or update) an ability for this session. Identity is (modUniqueId, abilityId).
        /// Curves:
        ///   linear → curveData: { xpPerLevel: int }
        ///   step   → curveData: { base: int, step: int }
        ///   table  → curveData: { levels: int[] }
        /// Levels are computed from persisted total EXP at runtime; only total EXP is saved.
        /// </summary>
        /// <param name="modUniqueId">Your mod's unique ID (IManifest.UniqueID).</param>
        /// <param name="abilityId">Stable ID unique within your mod.</param>
        /// <param name="displayName">Display name (localized as you see fit).</param>
        /// <param name="description">Description (localized as you see fit).</param>
        /// <param name="curveKind">"linear" | "step" | "table".</param>
        /// <param name="curveData">
        /// For "linear": { "xpPerLevel": int }.
        /// For "step":   { "base": int, "step": int }.
        /// For "table":  { "levels": int[] }.
        /// </param>
        /// <param name="maxLevel">
        /// Maximum level (cap). If using "table", effective cap is min(levels.Length, maxLevel).
        /// </param>
        /// <param name="iconPath">Optional icon path for UI.</param>
        /// <param name="tags">Optional tags for filtering.</param>
        void RegisterAbility(
            string modUniqueId,
            string abilityId,
            string displayName,
            string description,
            string curveKind,
            IDictionary<string, object> curveData,
            int maxLevel,
            string? iconPath = null,
            string[]? tags = null
        );

        int GetAbilityLevel(string modUniqueId, string abilityId);

        void GrantAbilityExp(string modUniqueId, string abilityId, int exp);

        (int expInto, int expNeeded, int maxLevel) GetAbilityProgress(string modUniqueId, string abilityId);

        bool IsAbilityAtMax(string modUniqueId, string abilityId);

        int GetAbilityRemainingXpToCap(string modId, string abilityId);

        /// <summary>
        /// Attempt to pay the given energy cost to cast an ability. Returns true if there is enough energy and then energy is consumed to cast; false if not enough no energy consumed.
        /// </summary>
        bool TryToUseAbility(float energyCost);
        float GetCurrentEnergy();
    }
}
