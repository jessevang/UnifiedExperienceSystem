using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;


namespace UnifiedExperienceSystem
{
    public partial class ModEntry
    {

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {

            startOfDayExp.Clear();
            startOfDayLevel.Clear();
            manuallyAllocatedLevels.Clear();
            skillList = LoadAllSkills();

            foreach (var skill in skillList)
            {
                int xp = GetExperience(Game1.player, skill);
                startOfDayExp[skill.Id] = xp;

                if (skill.IsVanilla)
                { 
                    startOfDayLevel[skill.Id] = Game1.player.GetUnmodifiedSkillLevel(int.Parse(skill.Id));
                }

                if (Config.DebugMode && skill.IsVanilla)
                    Monitor.Log($"[DayStart] Skill '{skill.DisplayName}' start of the day EXP: {xp} Start Of the Day Level: {startOfDayLevel[skill.Id].ToString() }", LogLevel.Debug);
                

            }
        }

        private void OnDayEnding(object sender, DayEndingEventArgs e)
        {

            //sets the skill gained throughout the day so level up screen appears
            foreach (var (skillIndex, level) in manuallyAllocatedLevels)
            {
                if (!Game1.player.newLevels.Contains(new Point(skillIndex, level)))
                    Game1.player.newLevels.Add(new Point(skillIndex, level));

            }
        }


        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            //use to update drawing the button being dragged
            if (isHoldingButton)
            {
                holdTimer += (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds;

                if (holdTimer >= HoldDelaySeconds)
                {
                    CheckButtonDragging();
                }
            }



            if (!e.IsMultipleOf((uint)Config.UpdateIntervalTicks))
                return;


            foreach (var skill in skillList)
            {
                int currentXP = GetExperience(Game1.player, skill);
                int baseXP = startOfDayExp.GetValueOrDefault(skill.Id, -1);
                int delta = 0;

                if (currentXP > baseXP && baseXP != -1)
                {
                    delta = currentXP - baseXP;
                }

                if (delta > 0)
                {
                    SaveData.GlobalEXP += delta;

                    if (Config.DebugMode)
                        Monitor.Log($"[EXP Transfer] {skill.DisplayName}: +{delta} EXP => GlobalEXP = {SaveData.GlobalEXP}", LogLevel.Debug);

                    if (skill.IsVanilla)
                    {
                        int expectedLevel = startOfDayLevel.GetValueOrDefault(skill.Id, -1);

                        if (expectedLevel >= 0)
                        {
                            if (skill.DisplayName.Equals("Farming") && Game1.player.farmingLevel.Get() > expectedLevel)
                                Game1.player.farmingLevel.Set(expectedLevel);
                            if (skill.DisplayName.Equals("Fishing") && Game1.player.fishingLevel.Get() > expectedLevel)
                                Game1.player.fishingLevel.Set(expectedLevel);
                            if (skill.DisplayName.Equals("Foraging") && Game1.player.foragingLevel.Get() > expectedLevel)
                                Game1.player.foragingLevel.Set(expectedLevel);
                            if (skill.DisplayName.Equals("Mining") && Game1.player.miningLevel.Get() > expectedLevel)
                                Game1.player.miningLevel.Set(expectedLevel);
                            if (skill.DisplayName.Equals("Combat") && Game1.player.combatLevel.Get() > expectedLevel)
                                Game1.player.combatLevel.Set(expectedLevel);
                            if (skill.DisplayName.Equals("Luck") && Game1.player.luckLevel.Get() > expectedLevel)
                                Game1.player.luckLevel.Set(expectedLevel);
                        }

                        if ((skill.IsVanilla || !skill.IsVanilla) && int.TryParse(skill.Id, out int skillIndex))
                        {
                            Game1.player.experiencePoints[skillIndex] = baseXP;

                            for (int i = Game1.player.newLevels.Count - 1; i >= 0; i--)
                            {
                                if (Game1.player.newLevels[i].X == skillIndex)
                                    Game1.player.newLevels.RemoveAt(i);
                            }

                            if (Config.DebugMode)
                                Monitor.Log($"[EXP Revert] {skill.DisplayName} reset to {baseXP} EXP", LogLevel.Trace);
                        }
                    }
                    else if (!skill.IsVanilla && spaceCoreApi != null)
                    {
                        spaceCoreApi.AddExperienceForCustomSkill(Game1.player, skill.Id, -delta);

                        if (Config.DebugMode)
                            Monitor.Log($"[EXP Revert - Custom] {skill.DisplayName} reduced by {delta} EXP", LogLevel.Trace);
                    }
                }
            }

            int pointsGained = 0;
            while (SaveData.GlobalEXP >= EXP_PER_POINT && !isAllocatingPoint)
            {
                SaveData.GlobalEXP -= EXP_PER_POINT;
                SaveData.UnspentSkillPoints++;
                pointsGained++;
            }

            if (pointsGained > 0 && Config.DebugMode)
                Monitor.Log($"[Skill Point Conversion] {pointsGained} skill point(s) granted. Remaining GlobalEXP: {SaveData.GlobalEXP}. Total Unspent Points: {SaveData.UnspentSkillPoints}", LogLevel.Debug);
        }





        public void AllocateSkillPoint(string skillId)
        {
            if (SaveData.UnspentSkillPoints <= 0)
                return;

            isAllocatingPoint = true;

            // Intended points to spend this click (respect config but don't exceed what we have)
            int pointsThatCanBeAllocated =
                SaveData.UnspentSkillPoints - Config.PointsAllocatedPerClick >= 0
                    ? Config.PointsAllocatedPerClick
                    : SaveData.UnspentSkillPoints;

            try
            {
                var skill = skillList.Find(s => s.Id == skillId);
                if (skill == null)
                    return;

                // --- DYNAMIC CAP DISCOVERY (respects patched curves) ---
                // Find the highest defined level by probing Farmer.getBaseExperienceForLevel.
                // If truly uncapped (mods), we stop at a probe ceiling and treat as uncapped.
                const int ProbeCeiling = 200; // safety stop for "infinite" curves
                int maxDefinedLevel = -1;
                for (int L = 1; L <= ProbeCeiling; L++)
                {
                    int thr = Farmer.getBaseExperienceForLevel(L);
                    if (thr < 0)
                    {
                        maxDefinedLevel = L - 1;
                        break;
                    }
                }
                bool isUncapped = (maxDefinedLevel < 0); // never hit -1 within ProbeCeiling

                // Current XP in this skill
                int currentXp = GetExperience(Game1.player, skill);

                // If capped, compute remaining room to the cap
                int capXp = 0;
                int room = int.MaxValue;
                if (!isUncapped)
                {
                    capXp = Farmer.getBaseExperienceForLevel(maxDefinedLevel); // cumulative XP at max level
                    room = Math.Max(0, capXp - currentXp);
                    if (room <= 0)
                    {
                        // already at (or beyond) cap — do nothing and don't spend a point
                        if (Config.DebugMode)
                            Monitor.Log($"[{nameof(AllocateSkillPoint)}] {skill.DisplayName} already at XP cap ({capXp}).", LogLevel.Trace);
                        return;
                    }
                }

                // How much XP a single point grants
                int xpPerPoint = EXP_PER_POINT;

                // Cap-aware calculation for how many points can actually be used this click
                int pointsUsed;
                int grantXp;

                if (isUncapped)
                {
                    // No cap: use all intended points
                    pointsUsed = pointsThatCanBeAllocated;
                    grantXp = xpPerPoint * pointsUsed;
                }
                else
                {
                    // Cap present: clamp to remaining room
                    int maxPointsByRoom = (int)Math.Ceiling(room / (double)xpPerPoint);
                    pointsUsed = Math.Min(pointsThatCanBeAllocated, maxPointsByRoom);

                    if (pointsUsed <= 0)
                    {
                        if (Config.DebugMode)
                            Monitor.Log($"[{nameof(AllocateSkillPoint)}] pointsUsed<=0 (room={room}).", LogLevel.Trace);
                        return;
                    }

                    int requestedXp = xpPerPoint * pointsUsed;
                    grantXp = Math.Min(requestedXp, room);
                }
                // --- END DYNAMIC CAP DISCOVERY ---

                int oldLevel = GetSkillLevel(Game1.player, skill);

                // Grant exactly the clamped XP
                AddExperience(Game1.player, skill, grantXp);

                int newLevel = GetSkillLevel(Game1.player, skill);
                if (newLevel > oldLevel)
                {
                    Game1.playSound("powerup"); // or "levelUp", "reward", etc.
                }

                // Track manually allocated vanilla levels for your respec/rollback logic
                if (skill.IsVanilla && int.TryParse(skill.Id, out int index))
                {
                    for (int i = oldLevel + 1; i <= newLevel; i++)
                        manuallyAllocatedLevels.Add((index, i));
                }

                // snapshot start-of-day after changes
                foreach (var s in skillList)
                {
                    startOfDayExp[s.Id] = GetExperience(Game1.player, s);
                    startOfDayLevel[s.Id] = GetSkillLevel(Game1.player, s);
                }

                // Spend only the points actually used
                SaveData.UnspentSkillPoints -= pointsUsed;

                if (Config.DebugMode)
                {
                    if (isUncapped)
                        Monitor.Log($"Allocated {grantXp} XP to {skill.DisplayName} (uncapped). Level {oldLevel} -> {newLevel}. Points used: {pointsUsed}. Points left: {SaveData.UnspentSkillPoints}", LogLevel.Debug);
                    else
                        Monitor.Log($"Allocated {grantXp} XP to {skill.DisplayName} (cap {capXp}). Level {oldLevel} -> {newLevel}. Points used: {pointsUsed}. Points left: {SaveData.UnspentSkillPoints}", LogLevel.Debug);
                }
            }
            finally
            {
                isAllocatingPoint = false;
            }
        }







    }
}
