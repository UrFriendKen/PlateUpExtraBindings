using Controllers;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace ExtraBindings.Patches
{
    [HarmonyPatch]
    internal static class Maps_Patch
    {
        public static HashSet<string> RejectedPaths = new HashSet<string>();

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
            __result.FindAction(Controls.Movement).RemoveAction();
            __result.AddAction(Controls.Movement);
            InputAction.ApplyDefaultBinding(__result.FindAction(Controls.Movement), DeviceType.Controller);

            BindingsRegistry.ApplyGamepadBindings(ref __result);

        }


        [HarmonyPatch(typeof(Maps), "NewKeyboard")]
        [HarmonyPostfix]
        static void NewKeyboard_Postfix(ref InputActionMap __result)
        {
            BindingsRegistry.ApplyKeyboardBindings(ref __result);
        }

        internal static int targetIndex = -1;

        [HarmonyPatch(typeof(Maps), "PerformRebinding", new Type[] { typeof(InputDevice), typeof(UnityEngine.InputSystem.InputAction), typeof(Action<RebindResult>) })]
        [HarmonyPrefix]
        public static bool PerformRebinding(InputDevice device, UnityEngine.InputSystem.InputAction action, Action<RebindResult> callback)
        {
            bool wasEnabled = action.enabled;
            Main.LogError("INTERACTIVE REBINDING");
            action.Disable();

            InputActionRebindingExtensions.RebindingOperation rebinding = action.PerformInteractiveRebinding();
            if (targetIndex > -1)
            {
                rebinding.WithTargetBinding(targetIndex);
            }

            HashSet<string> cancel_paths = new HashSet<string>();
            HashSet<string> exclude_paths = new HashSet<string>();
            cancel_paths.Add(fix_unity_path(action.actionMap.FindAction(Controls.MenuTrigger).bindings[0].effectivePath));
            exclude_paths.Add(fix_unity_path(action.actionMap.FindAction(Controls.MenuTrigger).bindings[0].effectivePath));
            foreach (string gameplayControl in new List<string> { Controls.Movement })
            {
                if (!(gameplayControl != action.name))
                {
                    continue;
                }
                foreach (InputBinding binding in action.actionMap.FindAction(gameplayControl).bindings)
                {
                    exclude_paths.Add(fix_unity_path(binding.effectivePath));
                }
            }
            rebinding.OnMatchWaitForAnother(-1f);
            rebinding.OnPotentialMatch(delegate (InputActionRebindingExtensions.RebindingOperation ro)
            {
                for (int num = ro.candidates.Count - 1; num >= 0; num--)
                {
                    InputControl inputControl = ro.candidates[num];
                    string item = fix_unity_path(inputControl.path);
                    if (cancel_paths.Contains(item))
                    {
                        rebinding.Cancel();
                        return;
                    }
                    if (exclude_paths.Contains(item))
                    {
                        callback(RebindResult.RejectedInUse);
                        ro.RemoveCandidate(inputControl);
                    }
                    else
                    {
                        bool synthetic = inputControl.synthetic;
                        bool flag = device != inputControl.device;
                        flag &= !(device.path == "/Keyboard") || !(inputControl.device.path == "/Mouse");
                        if (synthetic || flag)
                        {
                            ro.RemoveCandidate(inputControl);
                        }
                    }
                }
                if (ro.candidates.Count > 0)
                {
                    rebinding.Complete();
                }
            });
            rebinding.OnComplete(delegate (InputActionRebindingExtensions.RebindingOperation ro)
            {
                Main.LogError($"REBIND COMPLETED {ro.selectedControl} / {ro.action.bindings[0].overridePath}");
                callback(RebindResult.Success);
                rebinding.Dispose();
                BindingsRegistry.ActionEnabled(action, true);
            });
            rebinding.OnCancel(delegate (InputActionRebindingExtensions.RebindingOperation ro)
            {
                Main.LogError("REBIND FAILED");
                if (ro.canceled)
                {
                    callback(RebindResult.Cancelled);
                }
                if (!ro.canceled)
                {
                    callback(RebindResult.Fail);
                }
                rebinding.Dispose();
                BindingsRegistry.ActionEnabled(action, wasEnabled);
            });
            rebinding.Start();
            static string fix_unity_path(string path)
            {
                string[] array = path.Split('/');
                return array[array.Length - 1];
            }
            return false;
        }
    }
}
