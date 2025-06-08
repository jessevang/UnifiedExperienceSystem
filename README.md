
## Installation (for players)

1. Install the latest version of SMAPI API (https://www.nexusmods.com/stardewvalley/mods/2400)
2. Download and extract this mod into your 'Mods' folder
3. Launch the game using 'StardewModdingAPI.exe'

## Developer API Guide
This mod provides a public API to access and modify player experience, levels, points, & more.

Step 1: Add the API Interface

    Create a file in your mod project called IUnifiedExperienceAPI.cs.
    Copy the IUnifiedExperienceAPI.cs file from this repository into your IUnifiedExperienceAPI.cs


Step 2: Register the API
To register API add something like the follow below in your gameLaunched, SaveLoaded, or DayStarted event. Example of a gameLaunched event below
```csharp
public override void Entry(IModHelper helper)
{
    helper.Events.GameLoop.GameLaunched += OnGameLaunched;
}

private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
{

    //registers API
    var api = Helper.ModRegistry.GetApi<IUnifiedExperienceAPI>("UnifiedExperienceSystem");
    if (api != null)
    {
        Monitor.Log("Unified Experience API found!", LogLevel.Debug);
        //Add Logic, example of some calls:
            // --- Start-of-Day Data ---
        int combatXP = api.GetStartOfDayExp("Combat");
        api.SetStartOfDayLevel("Mining", 4);

        var allExp = api.GetAllStartOfDayExp();
        var allLevels = api.GetAllStartOfDayLevel();

        // --- Global EXP System ---
        int currentGlobalExp = api.GetGlobalEXP();
        int points = api.GetUnspentSkillPoints();

        api.SetGlobalEXP(currentGlobalExp + 500);
        api.SetUnspentSkillPoints(50);

        // Change EXP-per-point ratio (e.g., make leveling harder)
        api.SetEXPPerPoint(200);

        // List all tracked skill names
        foreach (var skill in api.GetAllSkillNames())
            Monitor.Log($"Tracking skill: {skill}", LogLevel.Debug);

    }

}
```
  
Step 3: Add This Mod as a Dependencies in your Manifesto
```json
"Dependencies": [
    {
    "UniqueID": "UnifiedExperienceSystem",
    "IsRequired": false
     "MinimumVersion": "1.0.7"
    }
]
```

Step 4: Use Case for this API
Once you've accessed the API, you can:

    Get the start of the day Skill's EXP at the start of the day.
    Get the start of the day level for a skill.
    Change the start of the day XP or level.
    View or replace the full list of recorded skills and values.
    Adjust start-of-day XP/level if your mod modifies these values to be lower or set back to 0 exp, becaues my global experience only adds skill exp that exceeds start of the day exp.
    Spend or grant global EXP or skill points as quest/event rewards
    Adjust level values when syncing with other mods that change player stats
    Let player reset skills experience by removing all experience then adding points to allow user to reuse all skill points.
       