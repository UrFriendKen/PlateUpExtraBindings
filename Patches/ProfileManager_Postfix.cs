using HarmonyLib;
using Kitchen;
using KitchenLib.Utils;
using System.Collections.Generic;

namespace ExtraBindings.Patches
{
    [HarmonyPatch]
    static class ProfileManager_Postfix
    {
        [HarmonyPatch(typeof(ProfileManager), nameof(ProfileManager.ApplyBindings))]
        [HarmonyPostfix]
        static void ApplyBindings(ref Dictionary<string, string> ___ControlOverrides, string name)
        {
            Main.LogInfo("--------- ProfileManager Logging Postfix ---------");
            if (name.IsNullOrEmpty())
            {
                Main.LogInfo("No profile name supplied");
                return;
            }
            if (!___ControlOverrides.TryGetValue(name, out string value))
            {
                Main.LogInfo($"{name} does not have ControlOverrides");
                return;
            }
            Main.LogInfo($"{name}:\n{value}");
        }
    }
}
