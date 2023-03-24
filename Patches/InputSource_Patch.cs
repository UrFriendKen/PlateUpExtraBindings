using Controllers;
using HarmonyLib;
using Kitchen;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace ExtraBindings.Patches
{
    [HarmonyPatch]
    static class InputSource_Patch
    {
        [HarmonyPatch(typeof(InputSource), nameof(InputSource.Update))]
        [HarmonyPostfix]
        static void Update_Postfix(ref InputSource __instance, ref Dictionary<int, PlayerData> ___Players, ref List<IInputConsumer> ___Consumers)
        {
            Dictionary<int, PlayerData>.KeyCollection keys = ___Players.Keys;
            using Dictionary<int, PlayerData>.KeyCollection.Enumerator enumerator = keys.GetEnumerator();
            PlayerData playerData;
            for (; enumerator.MoveNext();)
            {
                int current = enumerator.Current;
                playerData = ___Players[current];
                UserInputData inputData = playerData.InputData;
                if (Application.isFocused && !__instance.GlobalLock.IsLocked && !playerData.Lock.IsLocked)
                {
                    ReadOnlyArray<InputDevice>? devices = inputData.Map.devices;
                    if (devices.HasValue && devices.GetValueOrDefault().Count > 0)
                    {
                        BindingsRegistry.UpdateActionStates(current, inputData.Map);
                        continue;
                    }
                }
                BindingsRegistry.SetAllActionStatesNeutral(current);
            }
        }


        [HarmonyPatch(typeof(InputSource), "NewDeviceUsed")]
        [HarmonyPostfix]
        static void NewDeviceUsed_Postfix(ref Dictionary<int, PlayerData> ___Players)
        {
            foreach (PlayerData player in ___Players.Values)
            {
                foreach (UnityEngine.InputSystem.InputAction action in player.InputData.Map.actions)
                {
                    if (action.bindings.Count == 0)
                    {
                        BindingsRegistry.ActionEnabled(action, false, doNotSave: true);
                        InputAction.ApplyDefaultBinding(action, player.ControllerType);
                    }
                }
            }
        }
    }
}
