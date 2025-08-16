using System.Collections.Generic;

namespace UnifiedExperienceSystem
{
    /// <summary>
    /// Public API for interacting with the Unified Experience System's data.
    /// Allows other mods to access and modify the global EXP pool, skill point system,
    /// start-of-day skill data, and register/query abilities.
    /// </summary>
    public interface IUnifiedExperienceAPI
    {
        // --- Start-of-Day EXP and Level ---

        /// <summary>Gets the EXP value for a specific skill as recorded at the start of the current day.</summary>
        int GetStartOfDayExp(string skillName);

        /// <summary>Sets the EXP value for a specific skill at the start of the day.</summary>
        void SetStartOfDayExp(string skillName, int xp);

        /// <summary>Returns a dictionary of all skill names mapped to their EXP values at day start.</summary>
        IDictionary<string, int> GetAllStartOfDayExp();

        /// <summary>Sets all start-of-day EXP values in bulk (replaces the internal dictionary).</summary>
        void SetAllStartOfDayExp(IDictionary<string, int> dict);

        /// <summary>Gets the recorded level for a specific skill at the start of the day.</summary>
        int GetStartOfDayLevel(string skillName);

        /// <summary>Sets the level value for a skill at the start of the day.</summary>
        void SetStartOfDayLevel(string skillName, int level);

        /// <summary>Returns a dictionary of all skill names mapped to their level values at day start.</summary>
        IDictionary<string, int> GetAllStartOfDayLevel();

        /// <summary>Sets all start-of-day skill levels in bulk (replaces the internal dictionary).</summary>
        void SetAllStartOfDayLevel(IDictionary<string, int> dict);

        // --- Global EXP System ---

        /// <summary>Gets the current global EXP value.</summary>
        int GetGlobalEXP();

        /// <summary>Sets the current global EXP value.</summary>
        void SetGlobalEXP(int value);

        /// <summary>Gets the number of unspent skill points the player currently has.</summary>
        int GetUnspentSkillPoints();

        /// <summary>Sets the number of unspent skill points the player currently has.</summary>
        void SetUnspentSkillPoints(int value);

        /// <summary>Gets a list of all skill names tracked by the system (vanilla + custom).</summary>
        IEnumerable<string> GetAllSkillNames();

        // --- Abilities (new) ---

        // --- Abilities (new) ---

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

        /// <summary>
        /// Gets the current level for an ability derived from the persisted total EXP and the registered curve.
        /// Returns 0 if the ability isn't registered this session.
        /// </summary>
        int GetAbilityLevel(string modUniqueId, string abilityId);

        /// <summary>
        /// Adds EXP directly to an ability.
        /// </summary>
        void GrantAbilityExp(string modUniqueId, string abilityId, int exp);

        /// <summary>
        /// Gets progress numbers for the next level: how much EXP is into the current level and how much is needed,
        /// plus the max level for the ability. Returns (0,0,1) if not registered this session.
        /// </summary>
        (int expInto, int expNeeded, int maxLevel) GetAbilityProgress(string modUniqueId, string abilityId);

        /// <summary>
        /// Returns true if the ability is at its maximum level given the current curve and cap.
        /// </summary>
        bool IsAbilityAtMax(string modUniqueId, string abilityId);

        // (Optional write hooks; expose if you want partners to drive progression directly)
        // bool SpendPointsOnAbility(string modUniqueId, string abilityId, int points);
        // void GrantAbilityExp(string modUniqueId, string abilityId, int exp);
    }
}
