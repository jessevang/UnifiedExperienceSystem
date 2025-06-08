namespace UnifiedExperienceSystem
{
    /// <summary>
    /// Public API for interacting with the Unified Experience System's data.
    /// Allows other mods to access and modify the global EXP pool, skill point system,
    /// and start-of-day skill data.
    /// </summary>
    public interface IUnifiedExperienceAPI
    {
        // --- Start-of-Day EXP and Level ---

        /// <summary>
        /// Gets the EXP value for a specific skill as recorded at the start of the current day.
        /// </summary>
        /// <param name="skillName">The skill name (e.g., "Farming", "Mining", or a custom skill ID).</param>
        /// <returns>The recorded EXP at day start.</returns>
        int GetStartOfDayExp(string skillName);

        /// <summary>
        /// Sets the EXP value for a specific skill at the start of the day.
        /// </summary>
        /// <param name="skillName">The skill name to set.</param>
        /// <param name="xp">The EXP value to store.</param>
        void SetStartOfDayExp(string skillName, int xp);

        /// <summary>
        /// Returns a dictionary of all skill names mapped to their EXP values at day start.
        /// </summary>
        IDictionary<string, int> GetAllStartOfDayExp();

        /// <summary>
        /// Sets all start-of-day EXP values in bulk.
        /// Replaces the current internal dictionary.
        /// </summary>
        /// <param name="dict">A dictionary of skill names to EXP values.</param>
        void SetAllStartOfDayExp(IDictionary<string, int> dict);

        /// <summary>
        /// Gets the recorded level for a specific skill at the start of the day.
        /// </summary>
        /// <param name="skillName">The skill name.</param>
        /// <returns>The recorded level.</returns>
        int GetStartOfDayLevel(string skillName);

        /// <summary>
        /// Sets the level value for a skill at the start of the day.
        /// </summary>
        /// <param name="skillName">The skill name to set.</param>
        /// <param name="level">The level value to store.</param>
        void SetStartOfDayLevel(string skillName, int level);

        /// <summary>
        /// Returns a dictionary of all skill names mapped to their level values at day start.
        /// </summary>
        IDictionary<string, int> GetAllStartOfDayLevel();

        /// <summary>
        /// Sets all start-of-day skill levels in bulk.
        /// Replaces the current internal dictionary.
        /// </summary>
        /// <param name="dict">A dictionary of skill names to level values.</param>
        void SetAllStartOfDayLevel(IDictionary<string, int> dict);

        // --- Global EXP System ---

        /// <summary>
        /// Gets the current global EXP value available to the player.
        /// </summary>
        int GetGlobalEXP();

        /// <summary>
        /// Sets the global EXP value available to the player.
        /// </summary>
        /// <param name="value">The new global EXP total.</param>
        void SetGlobalEXP(int value);

        /// <summary>
        /// Gets the number of unspent skill points the player currently has.
        /// </summary>
        int GetUnspentSkillPoints();

        /// <summary>
        /// Sets the number of unspent skill points the player has.
        /// </summary>
        /// <param name="value">The new number of points.</param>
        void SetUnspentSkillPoints(int value);

        /// <summary>
        /// Gets the number of EXP required to earn one skill point.
        /// </summary>
        int GetEXPPerPoint();

        /// <summary>
        /// Sets the number of EXP required to earn one skill point.
        /// </summary>
        /// <param name="value">The EXP-per-point ratio to set.</param>
        void SetEXPPerPoint(int value);

        /// <summary>
        /// Gets a list of all skill names being tracked by the system.
        /// Includes vanilla and custom (SpaceCore) skills.
        /// </summary>
        IEnumerable<string> GetAllSkillNames();
    }
}
