using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
        // Existing (keep as-is for compatibility)
        IEnumerable<(string modId, string abilityId, string displayName, string description, int maxLevel)>
            ListRegisteredAbilities();

        // New: includes iconPath and tags (normalized to non-null)
        IEnumerable<(string modId, string abilityId, string displayName, string description, int maxLevel, string? iconPath, string[] tags)>
            ListRegisteredAbilitiesDetailed();



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
        /// <param name="iconPath">
        /// Optional icon path for UI. This should be a valid content pipeline path:
        ///   • Vanilla assets: e.g. "LooseSprites/Cursors".
        ///   • Mod assets: e.g. "assets/myicon.png" inside your mod folder.
        /// If null, a default fallback icon (magnifying glass from LooseSprites/Cursors) is used.
        /// NOTE: If the path refers to a shared spritesheet (like "LooseSprites/Cursors"),
        /// the entire sheet will be used unless you call the overload that provides an explicit source rectangle.
        /// </param>
        /// <param name="tags">Optional tags for filtering or categorization.</param>
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
        /// <param name="iconTexture">
        /// Optional icon texture for UI. If provided, takes priority over <paramref name="iconPath"/>.
        /// </param>
        /// <param name="iconSourceRect">
        /// Optional source rectangle within <paramref name="iconTexture"/>. If null, the entire texture is used.
        /// Required if you’re targeting a shared spritesheet (like "LooseSprites/Cursors") and you only want
        /// a specific icon region instead of the whole sheet.
        /// </param>
        /// <param name="iconPath">
        /// Optional icon path for UI (only used if <paramref name="iconTexture"/> is null).
        /// This should be a valid content pipeline path:
        ///   • Vanilla assets: e.g. "LooseSprites/Cursors".
        ///   • Mod assets: e.g. "assets/myicon.png" inside your mod folder.
        /// If null, a default fallback icon (magnifying glass from LooseSprites/Cursors) is used.
        /// </param>
        /// <param name="tags">Optional tags for filtering or categorization.</param>
        void RegisterAbility(
            string modUniqueId,
            string abilityId,
            string displayName,
            string description,
            string curveKind,
            IDictionary<string, object> curveData,
            int maxLevel,
            Microsoft.Xna.Framework.Graphics.Texture2D? iconTexture,
            Microsoft.Xna.Framework.Rectangle? iconSourceRect = null,
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
