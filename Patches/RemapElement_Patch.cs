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
    }
}
