

namespace UnifiedExperienceSystem
{


    public class SaveData
    {
        public int GlobalEXP { get; set; } = 0;
        public int UnspentSkillPoints { get; set; } = 0;

        public List<AbilitySaveData> Abilities { get; set; } = new();
    }
    public sealed class AbilitySaveData
    {
        public string ModGuid { get; set; } = "";
        public string AbilityId { get; set; } = "";
        public long TotalExpSpent { get; set; } = 0;
    }
}
