// ISpaceCoreApi.cs
// Interface definition for accessing the SpaceCore API
// Based on the public API documented and exposed by spacechase0 in the SpaceCore mod.
// https://github.com/spacechase0/StardewValleyMods/tree/develop/SpaceCore

using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace UnifiedExperienceSystem
{
    public interface ISpaceCoreApi
    {
        string[] GetCustomSkills();
        int GetLevelForCustomSkill(Farmer farmer, string skill);
        int GetBuffLevelForCustomSkill(Farmer farmer, string skill);
        int GetTotalLevelForCustomSkill(Farmer farmer, string skill);
        void AddExperienceForCustomSkill(Farmer farmer, string skill, int amount);
        int GetExperienceForCustomSkill(Farmer farmer, string skill);
        Texture2D GetSkillPageIconForCustomSkill(string skill);
        Texture2D GetSkillIconForCustomSkill(string skill);
        int GetProfessionId(string skill, string profession);
    }
}
