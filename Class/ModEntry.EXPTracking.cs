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

                // ===== VANILLA SKILLS: allow >10 using our curve =====
                if (skill.IsVanilla)
                {
                    int idx = int.Parse(skill.Id);
                    int maxL = Math.Clamp(Config.MaxSkillLevel, 10, 100);

                    // Current raw (unmodified) level + XP
                    int oldLevel = Game1.player.GetUnmodifiedSkillLevel(idx);
                    int currentXp = GetExperience(Game1.player, skill);

                    // Total XP cap for our configured max level
                    int capXp = UESgetBaseExperienceForLevel(maxL);
                    int room = Math.Max(0, capXp - currentXp);
                    if (room <= 0)
                    {
                        if (Config.DebugMode)
                            Monitor.Log($"[{nameof(AllocateSkillPoint)}] {skill.DisplayName} already at XP cap ({capXp}) for MaxSkillLevel={maxL}.", LogLevel.Trace);
                        return;
                    }

                    int xpPerPoint = EXP_PER_POINT;
                    int maxPointsByRoom = (int)Math.Ceiling(room / (double)xpPerPoint);
                    int pointsUsed = Math.Min(pointsThatCanBeAllocated, maxPointsByRoom);
                    if (pointsUsed <= 0) return;

                    int grantXp = Math.Min(pointsUsed * xpPerPoint, room);
                    int newXp = currentXp + grantXp;

                    // Compute resulting level using vanilla≤10 + UES>10 thresholds
                    int newLevel = UESlevelFromXp(newXp);

                    // Track for end-of-day vanilla level-up UI only up to 10
                    if (newLevel > oldLevel)
                    {
                        for (int i = oldLevel + 1; i <= newLevel && i <= 10; i++)
                            manuallyAllocatedLevels.Add((idx, i));
                    }
                    //


                    // Apply the two raw vanilla fields (no readers patched)
                    switch (idx)
                    {
                        case 0: Game1.player.farmingLevel.Set(newLevel); break;
                        case 1: Game1.player.fishingLevel.Set(newLevel); break;
                        case 2: Game1.player.foragingLevel.Set(newLevel); break;
                        case 3: Game1.player.miningLevel.Set(newLevel); break;
                        case 4: Game1.player.combatLevel.Set(newLevel); break;
                    }
                    Game1.player.experiencePoints[idx] = newXp;

                    EnqueueVanillaLevelUps(idx, oldLevel, newLevel);

                    if (newLevel > oldLevel)
                        Game1.playSound("powerup");

                    // Spend only points actually used
                    SaveData.UnspentSkillPoints -= pointsUsed;

                    // Refresh day-start snapshots so the revert loop won’t undo this
                    foreach (var s in skillList)
                    {
                        startOfDayExp[s.Id] = GetExperience(Game1.player, s);
                        startOfDayLevel[s.Id] = s.IsVanilla
                            ? Game1.player.GetUnmodifiedSkillLevel(int.Parse(s.Id))
                            : GetSkillLevel(Game1.player, s);
                    }

                    if (Config.DebugMode)
                        Monitor.Log($"Allocated {grantXp} XP to {skill.DisplayName}. Level {oldLevel} -> {newLevel}. Points used: {pointsUsed}. Left: {SaveData.UnspentSkillPoints}", LogLevel.Debug);

                    return; // IMPORTANT: do not fall through
                }

                // ===== CUSTOM / SPACECORE SKILLS: keep your existing logic =====

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
                int currentXp2 = GetExperience(Game1.player, skill);

                // If capped, compute remaining room to the cap
                int capXp2 = 0;
                int room2 = int.MaxValue;

                if (!isUncapped)
                {
                    capXp2 = Farmer.getBaseExperienceForLevel(maxDefinedLevel);
                    room2 = Math.Max(0, capXp2 - currentXp2);
                    if (room2 <= 0)
                    {
                        if (Config.DebugMode)
                            Monitor.Log($"[{nameof(AllocateSkillPoint)}] {skill.DisplayName} already at XP cap ({capXp2}).", LogLevel.Trace);
                        return;
                    }
                }

                int xpPerPoint2 = EXP_PER_POINT;
                int pointsUsed2;
                int grantXp2;

                if (isUncapped)
                {
                    pointsUsed2 = pointsThatCanBeAllocated;
                    grantXp2 = xpPerPoint2 * pointsUsed2;
                }
                else
                {
                    int maxPointsByRoom2 = (int)Math.Ceiling(room2 / (double)xpPerPoint2);
                    pointsUsed2 = Math.Min(pointsThatCanBeAllocated, maxPointsByRoom2);
                    if (pointsUsed2 <= 0)
                    {
                        if (Config.DebugMode)
                            Monitor.Log($"[{nameof(AllocateSkillPoint)}] pointsUsed<=0 (room={room2}).", LogLevel.Trace);
                        return;
                    }

                    int requestedXp2 = xpPerPoint2 * pointsUsed2;
                    grantXp2 = Math.Min(requestedXp2, room2);
                }
                // --- END DYNAMIC CAP DISCOVERY ---

                int oldLevel2 = GetSkillLevel(Game1.player, skill);

                // Grant exactly the clamped XP for custom/SpaceCore via your existing method
                AddExperience(Game1.player, skill, grantXp2);

                int newLevel2 = GetSkillLevel(Game1.player, skill);
                if (newLevel2 > oldLevel2)
                    Game1.playSound("powerup");

                // snapshot start-of-day after changes
                foreach (var s in skillList)
                {
                    startOfDayExp[s.Id] = GetExperience(Game1.player, s);
                    startOfDayLevel[s.Id] = GetSkillLevel(Game1.player, s);
                }

                // Spend only the points actually used
                SaveData.UnspentSkillPoints -= pointsUsed2;

                if (Config.DebugMode)
                {
                    if (isUncapped)
                        Monitor.Log($"Allocated {grantXp2} XP to {skill.DisplayName} (uncapped). Level {oldLevel2} -> {newLevel2}. Points used: {pointsUsed2}. Points left: {SaveData.UnspentSkillPoints}", LogLevel.Debug);
                    else
                        Monitor.Log($"Allocated {grantXp2} XP to {skill.DisplayName} (cap {capXp2}). Level {oldLevel2} -> {newLevel2}. Points used: {pointsUsed2}. Points left: {SaveData.UnspentSkillPoints}", LogLevel.Debug);
                }
            }
            finally
            {
                isAllocatingPoint = false;
            }
        }




        private void RebuildUesXpCurve()
        {
            _uesXpCurve = new List<int> { 0 }; // pad index 0

            // 1..10: take vanilla totals as-is (supports other mods patching these)
            for (int L = 1; L <= 10; L++)
                _uesXpCurve.Add(Farmer.getBaseExperienceForLevel(L));

            int maxL = Math.Clamp(Config?.MaxSkillLevel ?? 20, 10, 100);
            if (maxL <= 10) return;

            // post-10 parameters
            double step = Math.Max(1, (double)Config.BaseStepBeyond10);      // base step at L11
            double g = Math.Max(0.0, (double)Config.Beyond10GrowthPercent); // growth per level (0.05 = +5%)

            int lastTotal = _uesXpCurve[10]; // anchor at the runtime L10 total

            for (int L = 11; L <= maxL; L++)
            {
                // add current step (rounded to nearest int, never < 1)
                int add = Math.Max(1, (int)Math.Round(step));
                lastTotal += add;
                _uesXpCurve.Add(lastTotal);

                // grow step for the NEXT level (compounding)
                step *= (1.0 + g);
            }
        }


        public int UESgetBaseExperienceForLevel(int level)
        {
            if (_uesXpCurve == null || _uesXpCurve.Count == 0) RebuildUesXpCurve();
            int maxL = Math.Clamp(Config?.MaxSkillLevel ?? 20, 10, 100);
            level = Math.Clamp(level, 1, maxL);
            return _uesXpCurve[level];
        }

        // Find level from TOTAL XP (works up to MaxSkillLevel)
        private int UESlevelFromXp(int totalXp)
        {
            if (_uesXpCurve == null || _uesXpCurve.Count == 0) RebuildUesXpCurve();
            int lo = 0, hi = _uesXpCurve.Count - 1;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) >> 1;
                if (_uesXpCurve[mid] <= totalXp) lo = mid; else hi = mid - 1;
            }
            return lo;
        }





    }
}
