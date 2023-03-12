﻿using HarmonyLib;
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
            float closestDistance = 99f;
            float closestAngle = 0f;
            for (int i = 0; i < 2; i++)
            {
                if (i > 0 && closestDistance != 99f && closestAngle != 0f)
                {
                    break;
                }
                
                foreach (ModuleInstance module in modules)
                {
                    if (module == selected || !module.Module.IsSelectable)
                    {
                        continue;
                    }
                    Vector2 position2 = module.Position;
                    Vector2 vector = position2 - position;
                    float magnitude = vector.magnitude;
                    float angleMatch = (vector.x * direction.x + vector.y * direction.y) / magnitude;
                    float sqrMagnitude = vector.sqrMagnitude;
                    switch (i)
                    {
                        case 0:
                            if (angleMatch > 0.999999f && magnitude < 5f)
                            {
                                if (angleMatch >= closestAngle && sqrMagnitude < closestDistance)
                                {
                                    closestDistance = sqrMagnitude;
                                    closestAngle = angleMatch;
                                    selected = module;
                                }
                            }
                            break;
                        case 1:
                            if (angleMatch > 0f)
                            {
                                if (sqrMagnitude < closestDistance)
                                {
                                    closestDistance = sqrMagnitude;
                                    closestAngle = angleMatch;
                                    selected = module;
                                }
                            }
                            break;
                    }
                }
                Main.LogInfo($"Angle({i}): {closestAngle}");
                Main.LogInfo($"Distance({i}): {closestDistance}");
            }
            Selected.SetValue(instance, selected);
        }
    }
}
