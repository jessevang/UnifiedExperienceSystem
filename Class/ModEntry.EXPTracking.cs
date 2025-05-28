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
            if (!Context.IsWorldReady || !e.IsMultipleOf((uint)Config.UpdateIntervalTicks))
                return;

            foreach (var skill in skillList)
            {
                int currentXP = GetExperience(Game1.player, skill);
                int baseXP = startOfDayExp.GetValueOrDefault(skill.Id, -1);
                int delta = 0;

                if (currentXP > baseXP && baseXP !=-1)
                {
                    delta = currentXP - baseXP;
                }

                if (delta > 0)
                {
    
                    SaveData.GlobalEXP += delta;


                    if (skill.IsVanilla)
                    {


                       int expectedLevel = startOfDayLevel.GetValueOrDefault(skill.Id, -1);
                        //setting level using Game1.player.setSkillLevel, so just manualling setting these levels back to 0
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
                                {
                                    Game1.player.newLevels.RemoveAt(i);
                                   
                                }
                            }

                        }
                            

                    }
                    else if (!skill.IsVanilla && spaceCoreApi != null)
                    {

                        spaceCoreApi.AddExperienceForCustomSkill(Game1.player, skill.Id, -delta);

                    }

                    
                    
                      
                }
            }

            // Apply EXP → Skill Point conversion 
            while (SaveData.GlobalEXP >= EXP_PER_POINT)
            {
                SaveData.GlobalEXP -= EXP_PER_POINT;
                SaveData.UnspentSkillPoints++;
            }
        }




        public void AllocateSkillPoint(string skillId)
        {
            if (SaveData.UnspentSkillPoints <= 0)
                return;

            var skill = skillList.Find(s => s.Id == skillId);
            if (skill == null)
                return;

            int oldLevel = GetSkillLevel(Game1.player, skill);

            AddExperience(Game1.player, skill, EXP_PER_POINT); // handles both vanilla + spacecore


            int newLevel = GetSkillLevel(Game1.player, skill);

            
            if ((skill.IsVanilla||!skill.IsVanilla) && int.TryParse(skill.Id, out int index))
            {
                for (int i = oldLevel + 1; i <= newLevel; i++)
                {
                    manuallyAllocatedLevels.Add((index, i));
                }
            }

           
            startOfDayExp[skill.Id] += EXP_PER_POINT;
            startOfDayLevel[skill.Id] = newLevel;
            SaveData.UnspentSkillPoints--;

         




        }




    }
}
