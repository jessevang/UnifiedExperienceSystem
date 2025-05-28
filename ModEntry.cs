using GenericModConfigMenu;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using System.Collections.Generic;

namespace UnifiedExperienceSystem
{

    /*
     * Unified Experience System Mod - Summary
     * 
     * 1. Replaces individual skill EXP gain with a shared global experience pool.
     * 2. Tracks skill EXP and levels (vanilla + SpaceCore) at the start of each day.
     * 3. Intercepts newly gained EXP during gameplay for vanilla skills, transfers it to the global pool.
     * 4. Provides a custom UI to allocate EXP points manually to any skill (allocation is permanent).
     * 5. Suppresses the default level-up screen at the end of the day by detecting level gains and clearing them.(currently only Vanilla is being done as spacecore has no API)
     */

    public class ModConfig
    {
        public KeybindList ToggleMenuKeys { get; set; } = new(
             new Keybind(SButton.F2),
             new Keybind(SButton.LeftTrigger, SButton.RightTrigger)
         );
        public bool ShowSkillPointButton { get; set; } = true;
        public int UpdateIntervalTicks { get; set; } = 6;
        public bool LuckSkillIsEnabled { get; set; } = false;
        public bool DebugMode { get; set; } = false;
    }

    public class SkillEntry
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public bool IsVanilla { get; set; }
    }

    public partial class ModEntry : Mod
    {
        private ISpaceCoreApi spaceCoreApi;
        private int suppressUpdateTicks = 0;

        public int EXP_PER_POINT = 100;
        public SaveData SaveData { get; private set; } = new SaveData();
        public ModConfig Config { get; private set; }
        public Dictionary<string, int> startOfDayExp = new();
        private Dictionary<string, int> startOfDayLevel = new();
        private List<SkillEntry> skillList = new();
        private readonly HashSet<(int skillIndex, int level)> manuallyAllocatedLevels = new();




        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += (s, e) => LoadSaveData();
            helper.Events.GameLoop.DayEnding += (s, e) => SaveToFile();
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Display.RenderedHud += OnRenderedHud;
            helper.Events.Input.ButtonPressed += OnButtonPressed;


        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            RegisterGMCM();
            RegisterSpaceCore();


        }

        private void RegisterSpaceCore()
        {
            //registers Spacecore if it's on
            if (Helper.ModRegistry.IsLoaded("spacechase0.SpaceCore"))
            {
                spaceCoreApi = Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore");
                Monitor.Log(spaceCoreApi != null
                    ? "SpaceCore API loaded successfully."
                    : "Failed to load SpaceCore API.", LogLevel.Debug);
            }
        }

        private string GetVanillaSkillName(int index)
        {
            return index switch
            {
                0 => "Farming",
                1 => "Fishing",
                2 => "Foraging",
                3 => "Mining",
                4 => "Combat",
                5 => "Luck",
                _ => "Unknown"
            };
        }

        private void LoadSaveData()
        {
            SaveData = Helper.Data.ReadSaveData<SaveData>("PlayerExpData") ?? new SaveData();
        }

        private void SaveToFile()
        {
            Helper.Data.WriteSaveData("PlayerExpData", SaveData);
        }

        private void RegisterGMCM()
        {
            var gmcm = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (gmcm is null)
                return;

            gmcm.Register(
                mod: ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => Helper.WriteConfig(Config)
            );

            gmcm.AddKeybindList(
                mod: ModManifest,
                name: () => "Toggle Menu Hotkeys",
                tooltip: () => "Keys that open the Unified Experience menu",
                getValue: () => Config.ToggleMenuKeys,
                setValue: value => Config.ToggleMenuKeys = value
            );

            gmcm.AddBoolOption(
                mod: ModManifest,
                name: () => "Show Skill Point Button",
                tooltip: () => "Whether to display the skill button on the HUD toolbar.",
                getValue: () => Config.ShowSkillPointButton,
                setValue: value => Config.ShowSkillPointButton = value
            );

            gmcm.AddNumberOption(
                mod: ModManifest,
                name: () => "EXP Interval Check",
                tooltip: () => "Number of game ticks between experience checks (60 ticks = 1 second).\nDefault is 6 ticks = 0.1 seconds.",
                getValue: () => Config.UpdateIntervalTicks,
                setValue: value => Config.UpdateIntervalTicks = value,
                min: 1,
                max: 60,
                interval: 1
            );

            gmcm.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Luck Skill",
                tooltip: () => "Include the Luck skill in the skill menu and EXP tracking",
                getValue: () => Config.LuckSkillIsEnabled,
                setValue: value => Config.LuckSkillIsEnabled = value
            );
            
            gmcm.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Debug Logging",
                tooltip: () => "Enables detailed logging for skill EXP changes, point allocation, and skill tracking.",
                getValue: () => Config.DebugMode,
                setValue: value => Config.DebugMode = value
            );

        }

        public List<SkillEntry> LoadAllSkills()
        {
            var result = new List<SkillEntry>();

            // 1. Vanilla skills
            for (int i = 0; i <= 5; i++)
            {
                result.Add(new SkillEntry
                {
                    Id = i.ToString(),
                    DisplayName = GetVanillaSkillName(i),
                    IsVanilla = true
                });
            }

            // 2. Custom skills from SpaceCore
            if (spaceCoreApi != null)
            {
                foreach (var skillId in spaceCoreApi.GetCustomSkills())
                {

                    //cleans up skill display name before adding it
                    string friendlyName = skillId.Split('.').Last();
                    friendlyName = char.ToUpper(friendlyName[0]) + friendlyName.Substring(1); 
                    result.Add(new SkillEntry
                    {
                        Id = skillId,
                        DisplayName = friendlyName,
                        IsVanilla = false
                    });
                }
            }

            //used to add fake skills for testing UI
           /* for (int i = 0; i < 20; i++)
                result.Add(new SkillEntry { Id = $"TestSkill{i}", DisplayName = $"Test Skill {i}", IsVanilla = false });
           */
            
            return result;
        }


        public int GetExperience(Farmer farmer, SkillEntry skill)
        {
            if (skill.IsVanilla)
                return farmer.experiencePoints[int.Parse(skill.Id)];
            else if (spaceCoreApi != null)
                return spaceCoreApi.GetExperienceForCustomSkill(farmer, skill.Id);

            return 0;
        }

        public void AddExperience(Farmer farmer, SkillEntry skill, int amount)
        {
            int before = GetExperience(farmer, skill);

            if (skill.IsVanilla)
            {
                farmer.gainExperience(int.Parse(skill.Id), amount);

                int after = GetExperience(farmer, skill);
                int actualGained = after - before;

                if (actualGained > amount)
                {
                    
                    SetExperience(farmer, skill, before + amount);
                }
            }
            else if (spaceCoreApi != null)
            {
                spaceCoreApi.AddExperienceForCustomSkill(farmer, skill.Id, amount);
            }
        }


        //Manually sets EXP for Vanilla Skills if adding 100 and 200 was added for some reason.
        public void SetExperience(Farmer farmer, SkillEntry skill, int amount)
        {
            if (skill.IsVanilla)
            {
                int skillIndex = int.Parse(skill.Id);

                amount = Math.Max(0, amount);

                if (farmer.experiencePoints.Count > skillIndex)
                    farmer.experiencePoints[skillIndex] = amount;
                else
                {

                    while (farmer.experiencePoints.Count <= skillIndex)
                        farmer.experiencePoints.Add(0);

                    farmer.experiencePoints[skillIndex] = amount;
                }
            }

        }







        public int GetSkillLevel(Farmer farmer, SkillEntry skill)
        {
            if (skill.IsVanilla)
                return farmer.GetSkillLevel(int.Parse(skill.Id));
            else if (spaceCoreApi != null)
                return spaceCoreApi.GetLevelForCustomSkill(farmer, skill.Id);

            return 0;
        }





    }
}
