using HarmonyLib;
using Kitchen.Modules;

namespace ExtraBindings.Patches
{
    [HarmonyPatch]
    internal static class ControlRebindElement_Patch
    {
        [HarmonyPatch(typeof(ControlRebindElement), nameof(ControlRebindElement.Setup))]
        [HarmonyPostfix]
        static void Setup_Postfix(ControlRebindElement __instance)
        {
            BindingsRegistry.RegisterGlobalLocalisation();
            BindingsRegistry.AddRebindOptions(__instance);
        }
    }
}
