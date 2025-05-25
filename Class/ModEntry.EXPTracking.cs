using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Collections.Generic;

namespace UnifiedExperienceSystem
{
    public partial class ModEntry
    {

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            startOfDayExp.Clear();
            skillList = LoadAllSkills();

            foreach (var skill in skillList)
            {
                int xp = GetExperience(Game1.player, skill);
                startOfDayExp[skill.Id] = xp;
            }
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady || !e.IsMultipleOf((uint)Config.UpdateIntervalTicks))
                return;


            foreach (var skill in skillList)
            {
                int currentXP = GetExperience(Game1.player, skill);
                int baseXP = startOfDayExp.GetValueOrDefault(skill.Id, currentXP);
                int delta = currentXP - baseXP;

                if (delta > 0)
                {
                    // Subtract delta from the skill
                    if (skill.IsVanilla)
                    {
                        Game1.player.experiencePoints[int.Parse(skill.Id)] = baseXP;
                    }
                    else if (spaceCoreApi != null)
                    {
                        spaceCoreApi.AddExperienceForCustomSkill(Game1.player, skill.Id, -delta);
                    }

                    // Add to global EXP
                    SaveData.GlobalEXP += delta;

                    // Update snapshot to base value
                    startOfDayExp[skill.Id] = baseXP;

                    if (Config.DebugMode)
                    {
                        Monitor.Log($"[UnifiedEXP] Gained {delta} EXP in skill '{skill.DisplayName}' → transferred to global pool. New total: {SaveData.GlobalEXP}", LogLevel.Debug);
                    }
                }
            }



            while (SaveData.GlobalEXP >= EXP_PER_POINT)
            {
                SaveData.GlobalEXP -= EXP_PER_POINT;
                SaveData.UnspentSkillPoints++;
            }
        }

        public void AllocateSkillPoint(string skillId)
        {
            if (SaveData.UnspentSkillPoints <= 0) return;

            var skill = skillList.Find(s => s.Id == skillId);
            if (skill == null)
                return;

            AddExperience(Game1.player, skill, EXP_PER_POINT);
            startOfDayExp[skill.Id] = GetExperience(Game1.player, skill);
            SaveData.UnspentSkillPoints--;
        }
    }
}
