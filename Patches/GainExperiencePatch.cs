using System;
using System.Reflection;
using HarmonyLib;
using StardewValley;
using StardewModdingAPI;

namespace UnifiedExperienceSystem.Patches
{
    /// <summary>
    /// Divert only the portion of XP that did NOT actually apply (blocked = requested - applied).
    /// Works for vanilla skills 0..5. Robust against mods that return false in a prefix.
    /// </summary>
    [HarmonyPatch]
    internal static class GainExperiencePatch
    {
        internal struct State
        {
            public bool initialized;
            public int which;
            public int preTotal;
        }

        // Explicitly target Farmer.gainExperience(int which, int howMuch)
        static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(Farmer), "gainExperience", new[] { typeof(int), typeof(int) });

        // Run FIRST so we snapshot even if another prefix returns false later.
        [HarmonyPriority(Priority.First)]
        [HarmonyBefore(new[] { "DaLion.Professions" })]
        static void Prefix(Farmer __instance, ref int which, ref int howMuch, ref State __state)
        {
            try
            {
                __state.initialized =
                    ReferenceEquals(__instance, Game1.player) &&
                    which >= 0 && which <= 4; // vanilla only without Luck (removed 5)

                if (!__state.initialized)
                {
                    __state.which = -1;
                    return;
                }

                __state.which = which;
                __state.preTotal = __instance.experiencePoints[which];

                if (ModEntry.Instance?.Config.DebugMode == true)
                {
                    ModEntry.Instance.Monitor.Log(
                        $"[UES/Prefix] skill={SkillName(which)} requested(arg)={howMuch} preTotal={__state.preTotal}",
                        LogLevel.Debug);
                }
            }
            catch (Exception ex)
            {
                ModEntry.Instance?.Monitor.Log($"[UES/Prefix ERROR] {ex}", LogLevel.Error);
                __state.initialized = false;
            }
        }

        // Run LAST so we see final howMuch after other prefixes (even if original was skipped).
        [HarmonyPriority(Priority.VeryLow)]
        [HarmonyAfter(new[] { "DaLion.Professions" })]
        static void Postfix(Farmer __instance, int which, int howMuch, ref State __state)
        {
            try
            {
                if (!__state.initialized || which != __state.which)
                    return;

                // vanilla only (0..4) (Luck 5 is ignored)
                if (which < 0 || which > 4)
                    return;

                int preTotal = __state.preTotal;
                int postTotal = __instance.experiencePoints[which];

                int requested = Math.Max(0, howMuch);
                int applied = Math.Max(0, postTotal - preTotal);
                int blocked = Math.Max(0, requested - applied);

                var mod = ModEntry.Instance;
                if (mod?.Config.DebugMode == true)
                {
                    mod.Monitor.Log(
                        $"[UES/Postfix] skill={SkillName(which)} requested(final)={requested} applied={applied} " +
                        $"preTotal={preTotal} -> postTotal={postTotal} => blocked={blocked}",
                        LogLevel.Debug);
                }

                // Do NOT add to GlobalEXP here. Just buffer blocked to drain once per tick.
                if (blocked > 0 && mod != null)
                {
                    mod._blockedXpBuffer[which] += blocked;

                    if (mod.Config.DebugMode)
                        mod.Monitor.Log($"[UES/Postfix] buffered blocked XP: skill={SkillName(which)} +{blocked} (buffer now={mod._blockedXpBuffer[which]})",
                            LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                ModEntry.Instance?.Monitor.Log($"[UES/Postfix ERROR] {ex}", LogLevel.Error);
            }
        }


        private static string SkillName(int idx) => idx switch
        {
            0 => "Farming",
            1 => "Fishing",
            2 => "Foraging",
            3 => "Mining",
            4 => "Combat",
            _ => $"Skill{idx}"
        };
    }
}
