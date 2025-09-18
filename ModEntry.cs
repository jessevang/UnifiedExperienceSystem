using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using UnifiedExperienceSystem.Class;


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
     * 6. Adds API to add new abilities with new Menu. Each ability has their own XP tracked
     */

    public class ModConfig
    {
        public KeybindList ToggleMenuKeys { get; set; } = new(
            new Keybind(SButton.F2),
            new Keybind(SButton.LeftTrigger, SButton.RightTrigger)
        );

        public KeybindList ToggleAbilityMenuKeys { get; set; } = new(
            new Keybind(SButton.F3)
        );

        public int MaxSkillLevel { get; set; } = 20;
        public int BaseStepBeyond10 { get; set; } = 5000;
        public float Beyond10GrowthPercent { get; set; } = 0.00f;
        public bool AnchorPost10ToVanillaLevel10 { get; set; } = true;

        public bool ShowSkillPointButton { get; set; } = false;

        public int UpdateIntervalTicks { get; set; } = 6;
        public bool LuckSkillIsEnabled { get; set; } = false;
        public bool DebugMode { get; set; } = false;

        public int MenuWidth { get; set; } = 1100;
        public int MenuHeight { get; set; } = 700;
        public int SkillMenuVisibleRows { get; set; } = 5;     
        public int SkillMenuRowSpacing { get; set; } = 80;


        public int? ButtonPosX { get; set; } = 700;
        public int? ButtonPosY { get; set; } = 432;

        public int PointsAllocatedPerClick = 1;


        public bool ShowAbilityButton { get; set; } = false;
        public int? AbilityButtonPosX { get; set; } = 500;
        public int? AbilityButtonPosY { get; set; } = 432;

        
        //Energy Configrations

        public bool ShowEnergyBar { get; set; } = true;
        public int EnergyBarX { get; set; } = 48;   
        public int EnergyBarY { get; set; } = 540;

        public int EnergyBarWidth { get; set; } = 48;
        public int EnergyBarHeight { get; set; } = 250;

        public bool EnergyBarShowNumeric { get; set; } = true;
        public bool EnergyBarUseRelativePos { get; set; } = true;
        public float EnergyBarRelX { get; set; } = 0.93f; 
        public float EnergyBarRelY { get; set; } = 0.97f;


        public bool EnergyBarFollowVanillaHud { get; set; } = true;  
        public string EnergyBarAnchorTarget { get; set; } = "Health";

        public int EnergyBarAnchorOffsetX { get; set; } = -140;
        public int EnergyBarAnchorOffsetY { get; set; } = -104;


        //for troubleshooting
        private static string RectToStr(Microsoft.Xna.Framework.Rectangle? r)
    => r.HasValue ? $"[{r.Value.X},{r.Value.Y},{r.Value.Width},{r.Value.Height}]" : "(null)";

    }

    public class AbilityInfo
    {
        public string ModId { get; init; } = "";
        public string AbilityId { get; init; } = "";
        public string DisplayName { get; init; } = "";
        public string Description { get; init; } = "";
        public int MaxLevel { get; init; }
        public string? IconPath { get; init; }
        public string[]? Tags { get; init; }
        public int CurrentLevel { get; init; }
        public float TotalExp { get; init; }

        public Rectangle IconSourceRect { get; init; }
    }



    public class SkillEntry
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public bool IsVanilla { get; set; }
    }

    public partial class ModEntry : Mod
    {
        public static ModEntry? Instance { get; private set; }
        public ISpaceCoreApi spaceCoreApi { get; private set; }
        public const int EXP_PER_POINT = 100;
        public SaveData SaveData { get; private set; } = new SaveData();
        public ModConfig Config { get; private set; }
        public Dictionary<string, int> startOfDayExp { get; private set; } = new();
        public Dictionary<string, int> startOfDayLevel { get; private set; } = new();
        private List<SkillEntry> skillList = new();
        private readonly HashSet<(int skillIndex, int level)> manuallyAllocatedLevels = new();
        private bool isAllocatingPoint = false;
        private UnifiedExperienceAPI apiInstance;

        private readonly Dictionary<(string modGuid, string abilityId), long> _totalExpByAbility = new();

        private IUnifiedExperienceAPI uesApi;

        private List<int> _uesXpCurve;


        //Energy
        private bool EnergyCanRegenerateAgain = true;

        internal readonly int[] _blockedXpBuffer = new int[6];
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
            helper.Events.Input.ButtonReleased += OnButtonReleased;

            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;

            HookAbilityToolbarEvents(helper);

            Helper.ConsoleCommands.Add("ues_energy_set", "Set energy value 0..100 (e.g. ues_energy_set 55)", (n, a) =>
            {
                if (a.Length > 0 && float.TryParse(a[0], out float v))
                {
                    _energy.Set(v);
                    Monitor.Log($"Energy => {_energy.Current}", LogLevel.Info);
                }
            });
            Instance = this;
            var harmony = new Harmony(ModManifest.UniqueID);
            harmony.PatchAll();


        }

        //added for API Support
        public override object GetApi()
        {
            return apiInstance ??= new UnifiedExperienceAPI(this);
        }
        internal sealed class ModAbilityIdComparer : IEqualityComparer<(string modId, string abilityId)>
        {
            public bool Equals((string modId, string abilityId) x, (string modId, string abilityId) y) =>
                string.Equals(x.modId, y.modId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.abilityId, y.abilityId, StringComparison.OrdinalIgnoreCase);

            public int GetHashCode((string modId, string abilityId) obj) =>
                HashCode.Combine(
                    StringComparer.OrdinalIgnoreCase.GetHashCode(obj.modId ?? string.Empty),
                    StringComparer.OrdinalIgnoreCase.GetHashCode(obj.abilityId ?? string.Empty)
                );
        }
        private static readonly IEqualityComparer<(string modId, string abilityId)> IdTupleComparer = new ModAbilityIdComparer();


        //used to reference all data from registered info to saved data
        public List<AbilityInfo> GetAllAbilityInfos(IUnifiedExperienceAPI? api)
        {

            var registry = (api?.ListRegisteredAbilities() ?? Enumerable.Empty<(string modId, string abilityId, string displayName, string Description, int maxLevel)>());


            var saves = this.SaveData.Abilities ?? new List<AbilitySaveData>();
            var saveLookup = saves
                .Where(a => !string.IsNullOrWhiteSpace(a.ModGuid) && !string.IsNullOrWhiteSpace(a.AbilityId))
                .ToDictionary(
                    a => (a.ModGuid!, a.AbilityId!),
                    a => Math.Max(0L, a.TotalExpSpent), 
                    IdTupleComparer
                );

            var infos = new List<AbilityInfo>();

            foreach (var (modId, abilityId, displayName, description, maxLevel) in registry)
            {
                // pull saved EXP if present
                saveLookup.TryGetValue((modId, abilityId), out long totalExp);

                // current level from API (int)
                int currentLevel = api?.GetAbilityLevel(modId, abilityId) ?? 0;

                infos.Add(new AbilityInfo
                {
                    ModId = modId,
                    AbilityId = abilityId,
                    DisplayName = displayName,
                    Description = description,
                    MaxLevel = maxLevel,
                    CurrentLevel = currentLevel,
                    TotalExp = totalExp
                });
            }

            return infos;
        }





        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            SaveData = new SaveData();

            if (Config.DebugMode)
                Monitor.Log("[UnifiedXP] Returned to title — SaveData cleared from memory.", LogLevel.Debug);
        }


        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            RegisterGMCM();
            RegisterSpaceCore();
            RegisterIconicFramework();
            uesApi = Helper.ModRegistry.GetApi<IUnifiedExperienceAPI>("Darkmushu.UnifiedExperienceSystem");
        }

        private void RegisterSpaceCore()
        {
            //registers Spacecore if it's on
            if (Helper.ModRegistry.IsLoaded("spacechase0.SpaceCore"))
            {
                spaceCoreApi = Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore");
                
                if (Config.DebugMode)
                    Monitor.Log(spaceCoreApi != null
                        ? "SpaceCore API loaded successfully."
                        : "Failed to load SpaceCore API.", LogLevel.Debug);
            }
        }

        private void RegisterIconicFramework()
        {
            var iconicFramework = Helper.ModRegistry.GetApi<IIconicFrameworkApi>("furyx639.ToolbarIcons");
            if (iconicFramework is null)
            {
                Monitor.Log("Iconic Framework not found, skipping toolbar icon registration.", LogLevel.Info);
                return;
            }

            ITranslationHelper I18n = Helper.Translation;

            iconicFramework.AddToolbarIcon(
                id: $"{ModManifest.UniqueID}.Skills",
                texturePath: "LooseSprites/Cursors",
                sourceRect: new Rectangle(391, 360, 11, 12),
                getTitle: () => I18n.Get("config.toggleMenuHotkeysSkill.name"),
                getDescription: () => I18n.Get("config.toggleMenuHotkeysSkill.tooltip"),
                onClick: () => Game1.activeClickableMenu = new SkillAllocationMenu(this)
            );

            iconicFramework.AddToolbarIcon(
                id: $"{ModManifest.UniqueID}.Abilities",
                texturePath: "LooseSprites/Cursors",
                sourceRect: new Rectangle(0, 410, 16, 16),
                getTitle: () => I18n.Get("config.toggleMenuHotkeysAbility.name"),
                getDescription: () => I18n.Get("config.toggleMenuHotkeysAbility.tooltip"),
                onClick: () => Game1.activeClickableMenu = new AbilityAllocationMenu(this)
            );
  

        }
       


        private string GetVanillaSkillName(int index)
        {


            // 0=Farming, 1=Fishing, 2=Foraging, 3=Mining, 4=Combat, 5=Luck
            try
            {
                if (index == 5)
                {
                    return "Unknown";
                }
                var name = Farmer.getSkillDisplayNameFromIndex(index);
                if (!string.IsNullOrWhiteSpace(name))
                    return name;
            }
            catch
            {

            }

            // Fallbacks
            return index switch
            {
                0 => "Farming",
                1 => "Fishing",
                2 => "Foraging",
                3 => "Mining",
                4 => "Combat",
                //5 => "Luck",
                _ => "Unknown"
            };
        }




        private void LoadSaveData()
        {
            SaveData = Helper.Data.ReadSaveData<SaveData>("PlayerExpData") ?? new SaveData();

            // ensure not null for old saves
            if (SaveData.Abilities == null)
                SaveData.Abilities = new List<AbilitySaveData>();

            //initialize Energy after a save game is loaded
            InitEnergyMinimal();

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

            ITranslationHelper T = Helper.Translation;

            gmcm.AddKeybindList(
               mod: ModManifest,
               name: () => T.Get("config.toggleMenuHotkeysSkill.name"),
               tooltip: () => T.Get("config.toggleMenuHotkeysSkill.tooltip"),
               getValue: () => Config.ToggleMenuKeys,
               setValue: value => Config.ToggleMenuKeys = value
           );

            gmcm.AddKeybindList(
               mod: ModManifest,
               name: () => T.Get("config.toggleMenuHotkeysAbility.name"),
               tooltip: () => T.Get("config.toggleMenuHotkeysAbility.tooltip"),
               getValue: () => Config.ToggleAbilityMenuKeys,
               setValue: value => Config.ToggleAbilityMenuKeys = value
            );


            
            gmcm.AddBoolOption(
                mod: ModManifest,
                name: () => T.Get("config.showSkillPointButton.name"),
                tooltip: () => T.Get("config.showSkillPointButton.tooltip"),
                getValue: () => Config.ShowSkillPointButton,
                setValue: value => Config.ShowSkillPointButton = value
            );

            gmcm.AddBoolOption(
                mod: ModManifest,
                name: () => T.Get("config.showAbilityPointButton.name"),
                tooltip: () => T.Get("config.showAbilityPointButton.tooltip"),
                getValue: () => Config.ShowAbilityButton,
                setValue: value => Config.ShowAbilityButton = value
            );


            gmcm.AddNumberOption(
                mod: ModManifest,
                name: () => T.Get("config.updateIntervalTicks.name"),
                tooltip: () => T.Get("config.updateIntervalTicks.tooltip"),
                getValue: () => Config.UpdateIntervalTicks,
                setValue: value => Config.UpdateIntervalTicks = value,
                min: 1, max: 60000, interval: 1
            );

            gmcm.AddNumberOption(
                mod: ModManifest,
                name: () => T.Get("config.pointsAllocatedPerClick.name"),
                tooltip: () => T.Get("config.pointsAllocatedPerClick.tooltip"),
                getValue: () => Config.PointsAllocatedPerClick,
                setValue: value => Config.PointsAllocatedPerClick = value,
                min: 1, max: 100, interval: 1
            );



            gmcm.AddBoolOption(
                mod: ModManifest,
                name: () => T.Get("config.debugMode.name"),
                tooltip: () => T.Get("config.debugMode.tooltip"),
                getValue: () => Config.DebugMode,
                setValue: value => Config.DebugMode = value
            );


            gmcm.AddPageLink(
                mod: ModManifest,
                pageId: "Experience Setting",
                text: () => T.Get("config.pageTitle.ExperienceSetting"),
                tooltip: () => T.Get("config.pageTitle.ExperienceSetting")
            );

            gmcm.AddPageLink(
                mod: ModManifest,
                pageId: "Menu Settings",
                text: () => T.Get("nav.menuSettings.name"),
                tooltip: () => T.Get("nav.menuSettings.tooltip")
            );
            gmcm.AddPageLink(
                mod: ModManifest,
                pageId: "Energy Settings",
                text: () => T.Get("nav.energySettings.name"),
                tooltip: () => T.Get("nav.energySettings.tooltip")
            );

            gmcmExperience(gmcm);
            gmcmSkillAndAbilityMenuSettings(gmcm);
            gmcmEnergyPage(gmcm);

        }

        private void gmcmExperience(IGenericModConfigMenuApi gmcm)
        {
            ITranslationHelper T = Helper.Translation;
            gmcm.AddPage(mod: ModManifest, pageId: "Experience Setting", pageTitle: () => T.Get("config.pageTitle.ExperienceSetting"));


            gmcm.AddNumberOption(
                mod: ModManifest,
                name: () => T.Get("config.maxSkillLevel.name"),
                tooltip: () => T.Get("config.maxSkillLevel.tooltip"),
                getValue: () => Config.MaxSkillLevel,
                setValue: v =>
                {
                    Config.MaxSkillLevel = Math.Clamp(v, 10, 100);
                    RebuildUesXpCurve();
                },
                min: 10, max: 100, interval: 1
            );

            gmcm.AddNumberOption(
                mod: ModManifest,
                name: () => T.Get("config.baseStepBeyond10.name"),
                tooltip: () => T.Get("config.baseStepBeyond10.tooltip"),
                getValue: () => Config.BaseStepBeyond10,
                setValue: v =>
                {
                    Config.BaseStepBeyond10 = Math.Max(1, v);
                    RebuildUesXpCurve();
                },
                min: 1000, max: 25000, interval: 100 
            );

            gmcm.AddNumberOption(
                mod: ModManifest,
                name: () => T.Get("config.beyond10GrowthPercent.name"),
                tooltip: () => T.Get("config.beyond10GrowthPercent.tooltip"),
                getValue: () => Config.Beyond10GrowthPercent,
                setValue: v =>
                {
                    Config.Beyond10GrowthPercent = Math.Max(0f, v); 
                    RebuildUesXpCurve();
                },
                min: 0f, max: 1f, interval: 0.01f 
            );
        }

        private void gmcmSkillAndAbilityMenuSettings(IGenericModConfigMenuApi gmcm)
        {
            ITranslationHelper T = Helper.Translation;
            gmcm.AddPage(mod: ModManifest, pageId: "Menu Settings", pageTitle: () => T.Get("page.menuSettings.title"));

            gmcm.AddNumberOption(
                 mod: ModManifest,
                 name: () => T.Get("config.menuWidth.name"),
                 tooltip: () => T.Get("config.menuWidth.tooltip"),
                 getValue: () => Config.MenuWidth,
                 setValue: value => Config.MenuWidth = value,
                 min: 400, max: 1600, interval: 10
             );

            gmcm.AddNumberOption(
                mod: ModManifest,
                name: () => T.Get("config.menuHeight.name"),
                tooltip: () => T.Get("config.menuHeight.tooltip"),
                getValue: () => Config.MenuHeight,
                setValue: value => Config.MenuHeight = value,
                min: 300, max: 1200, interval: 10
            );

            gmcm.AddNumberOption(
                mod: ModManifest,
                name: () => T.Get("config.skillMenuVisibleRows.name"),
                tooltip: () => T.Get("config.skillMenuVisibleRows.tooltip"),
                getValue: () => Config.SkillMenuVisibleRows,
                setValue: value => Config.SkillMenuVisibleRows = value,
                min: 1, max: 20, interval: 1
            );

            gmcm.AddNumberOption(
                mod: ModManifest,
                name: () => T.Get("config.skillMenuRowSpacing.name"),
                tooltip: () => T.Get("config.skillMenuRowSpacing.tooltip"),
                getValue: () => Config.SkillMenuRowSpacing,
                setValue: value => Config.SkillMenuRowSpacing = value,
                min: 30, max: 240, interval: 10
            );


        }
        private void gmcmEnergyPage(IGenericModConfigMenuApi gmcm)
        {
            ITranslationHelper i18n = Helper.Translation;
            gmcm.AddPage(mod: ModManifest, pageId: "Energy Settings", pageTitle: () => i18n.Get("page.energySettings.title"));

            gmcm.AddBoolOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.ShowEnergyBar.name"),
                tooltip: () => Helper.Translation.Get("config.ShowEnergyBar.tooltip"),
                getValue: () => Config.ShowEnergyBar,
                setValue: value => Config.ShowEnergyBar = value
            );

            gmcm.AddNumberOption(
                this.ModManifest,
                getValue: () => Config.EnergyBarAnchorOffsetX,
                setValue: v => Config.EnergyBarAnchorOffsetX = v,
                name: () => Helper.Translation.Get("config.EnergyBarAnchorOffsetX.name"),
                tooltip: () => Helper.Translation.Get("config.EnergyBarAnchorOffsetX.tooltip"),
                min: -1000, max: 200, interval: 1
            );



            gmcm.AddBoolOption(
                this.ModManifest,
                getValue: () => Config.EnergyBarShowNumeric,
                setValue: v => Config.EnergyBarShowNumeric = v,
                name: () => Helper.Translation.Get("config.EnergyBarShowNumeric.name"),
                tooltip: () => Helper.Translation.Get("config.EnergyBarShowNumeric.tooltip")
            );


        }



        public List<SkillEntry> LoadAllSkills()
        {
            var result = new List<SkillEntry>();

            // 1. Vanilla skills
            for (int i = 0; i <= 4; i++)
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
                    string skillname = spaceCoreApi.GetDisplayNameOfCustomSkill(skillId);
                    //cleans up skill display name before adding it
                    //Monitor.Log($"SkillName: {skillname}", LogLevel.Debug);
                    //string friendlyName = skillId.Split('.').Last();

                    //friendlyName = char.ToUpper(friendlyName[0]) + friendlyName.Substring(1); 
                    result.Add(new SkillEntry
                    {
                        Id = skillId,
                        DisplayName = skillname,
                        IsVanilla = false
                    });
                    
                    if(Config.DebugMode)
                        Monitor.Log($" Spacecore skillID:{skillId} DisplayName={skillname}", LogLevel.Info);

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
