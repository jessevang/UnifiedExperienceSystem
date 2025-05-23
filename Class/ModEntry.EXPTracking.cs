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
            for (int i = 0; i < 6; i++)
            {
                int xp = Game1.player.experiencePoints[i];
                startOfDayExp[i] = xp;
            }
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady || !e.IsMultipleOf(60)) return;

            for (int i = 0; i < 6; i++)
            {
                int currentXP = Game1.player.experiencePoints[i];
                int baseXP = startOfDayExp.GetValueOrDefault(i, currentXP);
                int delta = currentXP - baseXP;

                if (delta > 0)
                {
                    Game1.player.experiencePoints[i] = baseXP;
                    SaveData.GlobalEXP += delta;
                    startOfDayExp[i] = baseXP;
                }
            }

            while (SaveData.GlobalEXP >= EXP_PER_POINT)
            {
                SaveData.GlobalEXP -= EXP_PER_POINT;
                SaveData.UnspentSkillPoints++;
            }
        }

        public void AllocateSkillPoint(int skillIndex)
        {
            if (SaveData.UnspentSkillPoints <= 0 || skillIndex < 0 || skillIndex >= 6) return;

            Game1.player.gainExperience(skillIndex, EXP_PER_POINT);
            startOfDayExp[skillIndex] = Game1.player.experiencePoints[skillIndex];
            SaveData.UnspentSkillPoints--;
        }
    }
}
