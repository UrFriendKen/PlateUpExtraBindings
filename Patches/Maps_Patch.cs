using Controllers;
using HarmonyLib;
using UnityEngine.InputSystem;

namespace ExtraBindings.Patches
{
    [HarmonyPatch]
    static class Maps_Patch
    {
        [HarmonyPatch(typeof(Maps), "Actions")]
        [HarmonyPostfix]
        static void Actions_Postfix(ref InputActionMap __result)
        {
            BindingsRegistry.AddActionsToMap(ref __result);
        }


        [HarmonyPatch(typeof(Maps), "NewGamepad")]
        [HarmonyPostfix]
        static void NewGamepad_Postfix(ref InputActionMap __result)
        {
            BindingsRegistry.ApplyGamepadBindings(ref __result);
        }


        [HarmonyPatch(typeof(Maps), "NewKeyboard")]
        [HarmonyPostfix]
        static void NewKeyboard_Postfix(ref InputActionMap __result)
        {
            BindingsRegistry.ApplyKeyboardBindings(ref __result);
        }
    }
}
