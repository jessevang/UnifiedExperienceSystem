
## Installation (for players)

1. Install the latest version of SMAPI API (https://www.nexusmods.com/stardewvalley/mods/2400)
2. Download and extract this mod into your 'Mods' folder
3. Launch the game using 'StardewModdingAPI.exe'

## Developer API Guide
This mod provides a public API to access and register your mod's ability and obtain information around ability and skills.
Once Registered this mod appears in Ability Level List and can start gaining experience and level up.

Step 1: Add the API Interface

    Create a file in your mod project called IUnifiedExperienceAPI.cs.
    Copy the IUnifiedExperienceAPI.cs file from this repository into your IUnifiedExperienceAPI.cs


Step 2: Register the API with your Abilities
To register API see method for gameLaunched, To see API call to get your ability level see OnButtonPress Method.
```csharp
public class ModEntry : Mod
{
    private UnifiedExperienceSystem.IUnifiedExperienceAPI? uesApi;  

    public override void Entry(IModHelper helper)
    {
        helper.Events.Input.ButtonPressed += OnButtonPressed;
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    }

    private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
    {
            if (!Context.IsWorldReady)
                return;
            if (Config.WeaponFlyModeOnOffToggleHotkey.JustPressed())
            {
                int Abilitylevel = uesApi.GetAbilityLevel(this.ModManifest.UniqueID, "FlyingWeaponMountIgnoresCollision");

                if (Abilitylevel ==0)
                    return

                if (Abilitylevel >0)
                {
                    float manaCost = 40.0f;
                    if (useAPI.TryToUseAbility(manacost))
                    {
                        //Use Ability Function
                    }
                    else
                    {
                        //Not enough Energy do something else
                    }
                    
                }

                
            }
    }

    private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
    {
        RegisterUES();
    }

    private void RegisterUES()
    {
        //Registers Mod,
        uesApi = Helper.ModRegistry.GetApi<UnifiedExperienceSystem.IUnifiedExperienceAPI>("Darkmushu.UnifiedExperienceSystem");

        if (uesApi == null)
        {
            return;
        }

        // Linear curve — XP is fixed per level, need 1k to reach level 1, if level was set to 10, then need 10k xp to reach level 10.
        uesApi.RegisterAbility(
            modUniqueId: this.ModManifest.UniqueID,
            abilityId: "FlyingWeaponMountIgnoresCollision",
            displayName: "Flying Weapon Mount Flys Over Everything",
            description: "Unlocks ability fly anywhere.",
            curveKind: "linear",
            curveData: new Dictionary<string, object>
            {
        { "xpPerLevel", 1000 }
            },
            maxLevel: 1
        );

        //Step Curve - base 200xp, next level is 400+200, next one is 600+200... at level 10 it's 400+(10-1)*200 = 2200, total experience need to reach 10 is 13k xp
        uesApi.RegisterAbility(
            modUniqueId: this.ModManifest.UniqueID,
            abilityId: "FlyingWeaponMountSpeed",
            displayName: "Flying Weapon Mount Speed",
            description: "Increase weapon mount Speed by .25 per Level.",
            curveKind: "step",
            curveData: new Dictionary<string, object>
            {
                { "base", 400 },
                { "step", 200 }
            },
            maxLevel: 10
        );

        //Table Curve - Level 1 needs 100 XP, level 2 needs 200xp, level 3 need 300 xp, level 10 needs 1500xp, total xp to reach level 10 is 7300XP.
        uesApi.RegisterAbility(
            modUniqueId: this.ModManifest.UniqueID,
            abilityId: "FlyingWeaponMountStaminaDrain",
            displayName: "Reduce Stamina Drain",
            description: "Increase weapon mount Speed by .25 per Level",
            curveKind: "table",
            curveData: new Dictionary<string, object>
            {
                { "levels", new int[] { 100, 200, 300, 500, 600, 800, 1000, 1100, 1200, 1500 } }
            },
            maxLevel: 10
        );
    }

}
```
  
Step 3: Add This Mod as a Dependencies in your Manifesto
```json
"Dependencies": [
    {
    "UniqueID": "UnifiedExperienceSystem",
    "IsRequired": false
     "MinimumVersion": "1.1.0"
    }
]
```

Step 4: Use Case for this API
Once you've accessed the API, you can:

    Set XP for Skills or Abilities for your mod or other's mod that are registered
    Get Ability Levels to perform what your mod offers.
       