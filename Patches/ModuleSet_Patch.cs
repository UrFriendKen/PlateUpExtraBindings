using HarmonyLib;
using Kitchen.Modules;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ExtraBindings.Patches
{
    [HarmonyPatch]
    static class ModuleSet_Patch
    {
        static PropertyInfo Selected = typeof(ModuleSet).GetProperty("Selected", BindingFlags.Public | BindingFlags.Instance);

        [HarmonyPatch(typeof(ModuleSet), nameof(ModuleSet.MoveVertical))]
        [HarmonyPrefix]
        static bool MoveVertical_Prefix(ref ModuleSet __instance, bool up)
        {
            Move(__instance, up ? Vector2.up : Vector2.down);
            return false;
        }

        [HarmonyPatch(typeof(ModuleSet), nameof(ModuleSet.MoveHorizontal))]
        [HarmonyPrefix]
        static bool MoveHorizontal_Prefix(ref ModuleSet __instance, bool right)
        {
            Move(__instance, right ? Vector2.right : Vector2.left);
            return false;
        }

        static void Move(ModuleSet instance, Vector2 direction)
        {
            ModuleInstance selected = instance.Selected;
            List<ModuleInstance> modules = instance.Modules;
            if (selected == null)
            {
                return;
            }
            Vector2 position = selected.Position;
            direction = direction.normalized;
            float num = 99f;
            foreach (ModuleInstance module in modules)
            {
                if (module == selected || !module.Module.IsSelectable)
                {
                    continue;
                }
                Vector2 position2 = module.Position;
                Vector2 vector = position2 - position;
                float magnitude = vector.magnitude;
                float num2 = (vector.x * direction.x + vector.y * direction.y) / magnitude;
                if (num2 > 0f)
                {
                    float sqrMagnitude = vector.sqrMagnitude;
                    if (sqrMagnitude < num)
                    {
                        num = sqrMagnitude;
                        selected = module;
                    }
                }
            }
            Selected.SetValue(instance, selected);
        }
    }
}
