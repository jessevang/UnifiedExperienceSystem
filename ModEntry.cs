using GenericModConfigMenu;
using StardewModdingAPI;
using StardewModdingAPI.Events;

/* Unified Experience System - Core Logic
 * 1. Stores all skill XP at the start of the day.
 * 2. Per-tick monitoring to detect EXP gain in any skills
 * 3. Remove delta gained that exceeds start of day Exp and apply it to global EXP
 * 4. Dynamic UI generation for all skills (vanilla + modded)
 * 5. Skill point allocation updates skill's start of day EXP to reset base snapshot
*/


namespace UnifiedExperienceSystem
{
    public class ModConfig
    {
        public SButton ToggleMenuKey { get; set; } = SButton.F2;
        public bool LuckSkillIsEnabled { get; set; } = false;

    }


    public partial class ModEntry : Mod
    {
        public int EXP_PER_POINT = 100;
        public SaveData SaveData { get; private set; } = new SaveData();

        public ModConfig Config { get; private set; }

        public Dictionary<int, int> startOfDayExp = new();

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
        }
        public string GetSkillName(int index)
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
            //Loads global exp + points, if it doesn't exist, creates them with value of 0
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

            gmcm.AddKeybind(
                mod: ModManifest,
                name: () => "Toggle Menu Hotkey",
                tooltip: () => "Which key opens the Unified Experience menu",
                getValue: () => Config.ToggleMenuKey,
                setValue: value => Config.ToggleMenuKey = value
            );

            gmcm.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Luck Skill",
                tooltip: () => "Include the Luck skill in the skill menu and EXP tracking",
                getValue: () => Config.LuckSkillIsEnabled,
                setValue: value => Config.LuckSkillIsEnabled = value
            );
        }

    }
}


