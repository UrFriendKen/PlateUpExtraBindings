using HarmonyLib;
using Kitchen.Modules;
using TMPro;

namespace ExtraBindings.Patches
{
    [HarmonyPatch]
    static class RemapElement_Patch
    {
        [HarmonyPatch(typeof(RemapElement), "UpdateBinding")]
        [HarmonyPostfix]
        static void UpdateBinding_Postfix(ref int ___PlayerID, ref string ___Action, ref TextMeshPro ___InputPrompt)
        {
            if (!BindingsRegistry.ActionEnabled(___PlayerID, ___Action))
            {
                ___InputPrompt.text = string.Empty;
            }
        }

        [HarmonyPatch(typeof(RemapElement), "HandleBindingChange")]
        [HarmonyPrefix]
        static void HandleBindingChange_Prefix(ref bool __state, int i, string s, ref int ___PlayerID, ref string ___Action)
        {
            __state = BindingsRegistry.ActionEnabled(___PlayerID, ___Action);
            BindingsRegistry.ActionEnabled(___PlayerID, ___Action, enabled: true, doNotSave: true);
        }

        [HarmonyPatch(typeof(RemapElement), "HandleBindingChange")]
        [HarmonyPostfix]
        static void HandleBindingChange_Postfix(ref bool __state, ref int ___PlayerID, ref string ___Action)
        {
            BindingsRegistry.ActionEnabled(___PlayerID, ___Action, enabled: __state, doNotSave: true);
        }
    }
}
