using System.Collections.Generic;

namespace UnifiedExperienceSystem
{
    public class UnifiedExperienceAPI : IUnifiedExperienceAPI
    {
        private readonly ModEntry mod;

        public UnifiedExperienceAPI(ModEntry mod)
        {
            this.mod = mod;
        }

        // --- Start-of-day EXP and Level ---

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

        // --- Global EXP System ---

        public int GetGlobalEXP() => mod.SaveData.GlobalEXP;
        public void SetGlobalEXP(int value) => mod.SaveData.GlobalEXP = value;

        public int GetUnspentSkillPoints() => mod.SaveData.UnspentSkillPoints;
        public void SetUnspentSkillPoints(int value) => mod.SaveData.UnspentSkillPoints = value;

        public int GetEXPPerPoint() => mod.EXP_PER_POINT;
        public void SetEXPPerPoint(int value) => mod.EXP_PER_POINT = value;

        public IEnumerable<string> GetAllSkillNames()
        {
            var names = new List<string>();
            foreach (var skill in mod.LoadAllSkills())
                names.Add(skill.DisplayName);
            return names;
        }
    }
}
