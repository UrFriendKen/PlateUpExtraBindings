using Controllers;
using Kitchen.Modules;
using KitchenData;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ExtraBindings
{
    public static class BindingsRegistry
    {
        public class PlayerActionState
        {
            public Dictionary<string, Vector2> ValueActionStates { get; } = new Dictionary<string, Vector2>();
            public Dictionary<string, ButtonState> ButtonActionStates { get; } = new Dictionary<string, ButtonState>();

            internal bool GetValueActionState(string action, out Vector2 cached)
            {
                return ValueActionStates.TryGetValue(action, out cached);
            }

            internal bool GetButtonActionState(string action, out ButtonState cached)
            {
                return ButtonActionStates.TryGetValue(action, out cached);
            }

            internal void SetValueActionState(string action, Vector2 value)
            {
                if (!ValueActionStates.ContainsKey(action))
                {
                    ValueActionStates.Add(action, Vector2.zero);
                }
                ValueActionStates[action] = value;
            }

            internal void SetButtonActionState(string action, ButtonState value)
            {
                if (!ButtonActionStates.ContainsKey(action))
                {
                    ButtonActionStates.Add(action, ButtonState.Up);
                }
                ButtonActionStates[action] = value;
            }
        }

        static Dictionary<string, InputAction> Registered = new Dictionary<string, InputAction>();

        static Dictionary<int, PlayerActionState> ActionStatesCache = new Dictionary<int, PlayerActionState>();

        public static InputAction AddValueAction(string id, string displayName)
        {
            return AddAction(id, displayName, InputActionType.Value, allowRebind: false);
        }

        public static InputAction AddButtonAction(string id, string displayName)
        {
            return AddAction(id, displayName, InputActionType.Button);
        }

        private static InputAction AddAction(string id, string displayName, InputActionType inputType, bool allowRebind = true)
        {
            return AddAction(new InputAction(id, displayName, inputType, allowRebind));
        }

        public static InputAction AddAction(InputAction inputAction)
        {
            if (Registered.ContainsKey(inputAction.ID))
            {
                inputAction = Registered[inputAction.ID];
            }
            else
            {
                Registered.Add(inputAction.ID, inputAction);
            }
            return inputAction;
        }

        public static PlayerActionState GetPlayerActionStates(int playerId)
        {
            if (!ActionStatesCache.ContainsKey(playerId))
            {
                throw new ArgumentException($"Invalid playerId ({playerId})! Ensure playerId is correct and player is in the local session.", "playerId");
            }
            return ActionStatesCache[playerId];
        }

        public static Dictionary<string, ButtonState> GetPlayerButtonActionStates(int playerId)
        {
            return GetPlayerActionStates(playerId).ButtonActionStates;
        }

        public static Dictionary<string, Vector2> GetPlayerValueActionStates(int playerId)
        {
            return GetPlayerActionStates(playerId).ValueActionStates;
        }

        public static Dictionary<int, Vector2> GetValueActionStates(string action)
        {
            Dictionary<int, Vector2> result = new Dictionary<int, Vector2>();
            foreach (int playerId in ActionStatesCache.Keys)
            {
                result.Add(playerId, GetPlayerValueActionState(playerId, action));
            }
            return result;
        }

        public static Dictionary<int, ButtonState> GetButtonActionStates(string action)
        {
            Dictionary<int, ButtonState> result = new Dictionary<int, ButtonState>();
            foreach (int playerId in ActionStatesCache.Keys)
            {
                result.Add(playerId, GetPlayerButtonActionState(playerId, action));
            }
            return result;
        }

        public static Vector2 GetPlayerValueActionState(int playerId, string action)
        {
            return (Vector2)GetPlayerActionState(playerId, action, InputActionType.Value);
        }

        public static ButtonState GetPlayerButtonActionState(int playerId, string action)
        {
            return (ButtonState)GetPlayerActionState(playerId, action, InputActionType.Button);
        }

        private static object GetPlayerActionState(int playerId, string action, InputActionType actionType)
        {
            if (!Registered.TryGetValue(action, out InputAction inputAction))
            {
                throw new ArgumentException($"{action} is not a registered action!", "id");
            }
            if (inputAction.InputType != actionType)
            {
                string errorMsg = $"Wrong method used to retrieve action state for InputActionType.{inputAction.InputType}! ";
                switch (actionType)
                {
                    case InputActionType.Value:
                        errorMsg += "Use GetPlayerValueActionState(int playerId, string action) instead.";
                        break;
                    case InputActionType.Button:
                        errorMsg += "Use GetPlayerButtonActionState(int playerId, string action) instead.";
                        break;
                    case InputActionType.PassThrough:
                    default:
                        errorMsg = "Unsupported InputActionType.";
                        break;
                }
                throw new ApplicationException(errorMsg);
            }

            return ReadActionState(playerId, action, actionType);
        }


        internal static void AddRebindOptions(ControlRebindElement instance)
        {
            foreach (KeyValuePair<string, InputAction> kvp in Registered)
            {
                if (kvp.Value.AllowRebind)
                {
                    instance.AddRebindOption(kvp.Key, kvp.Key);
                }
            }
        }

        internal static void RegisterGlobalLocalisation()
        {
            foreach (KeyValuePair<string, InputAction> kvp in Registered)
            {
                if (!GameData.Main.GlobalLocalisation.Text.ContainsKey(kvp.Key))
                {
                    GameData.Main.GlobalLocalisation.Text.Add(kvp.Key, kvp.Value.Name);
                    Main.LogInfo($"Registered localisation for {kvp.Key}");
                }
            }
        }

        internal static void AddActionsToMap(ref InputActionMap inputActionMap)
        {
            foreach (KeyValuePair<string, InputAction> binding in Registered)
            {
                inputActionMap.AddAction(binding.Key, binding.Value.InputType);
            }
        }

        internal static void ApplyKeyboardBindings(ref InputActionMap inputActionMap)
        {
            foreach (KeyValuePair<string, InputAction> binding in Registered)
            {
                binding.Value.ApplyBindings(inputActionMap.FindAction(binding.Key), PeripheralType.Keyboard);
            }
        }

        internal static void ApplyGamepadBindings(ref InputActionMap inputActionMap)
        {
            foreach (KeyValuePair<string, InputAction> binding in Registered)
            {
                binding.Value.ApplyBindings(inputActionMap.FindAction(binding.Key), PeripheralType.Controller);
            }
        }

        internal static void UpdateActionStates(int playerId, InputActionMap inputActionMap)
        {
            if (!ActionStatesCache.ContainsKey(playerId))
            {
                ActionStatesCache.Add(playerId, new PlayerActionState());
            }
            foreach (KeyValuePair<string, InputAction> kvp in Registered)
            {
                switch (kvp.Value.InputType)
                {
                    case InputActionType.Value:
                        CacheValueActionState(playerId, kvp.Key, ReadValueActionState(playerId, inputActionMap, kvp.Key));
                        break;
                    case InputActionType.Button:
                        CacheButtonActionState(playerId, kvp.Key, ReadButtonActionState(playerId, inputActionMap, kvp.Key));
                        break;
                }
            }
        }

        internal static void SetAllActionStatesNeutral(int playerId)
        {
            if (!ActionStatesCache.ContainsKey(playerId))
            {
                ActionStatesCache.Add(playerId, new PlayerActionState());
            }
            foreach (KeyValuePair<string, InputAction> kvp in Registered)
            {
                switch (kvp.Value.InputType)
                {
                    case InputActionType.Value:
                        CacheValueActionState(playerId, kvp.Key, Vector2.zero);
                        break;
                    case InputActionType.Button:
                        CacheButtonActionState(playerId, kvp.Key, ButtonState.Up);
                        break;
                }
            }
        }

        private static void CacheValueActionState(int playerId, string action, Vector2 value)
        {
            ActionStatesCache[playerId].SetValueActionState(action, value);
        }

        private static void CacheButtonActionState(int playerId, string action, ButtonState value)
        {
            ActionStatesCache[playerId].SetButtonActionState(action, value);
        }

        internal static Vector2 ReadValueActionState(int playerId, InputActionMap inputActionMap, string id)
        {
            return (Vector2)ReadActionState(playerId, id, InputActionType.Value, inputActionMap);
        }

        internal static ButtonState ReadButtonActionState(int playerId, InputActionMap inputActionMap, string id)
        {
            return (ButtonState)ReadActionState(playerId, id, InputActionType.Button, inputActionMap);
        }

        private static object ReadActionState(int playerId, string id, InputActionType actionType, InputActionMap inputActionMap = null)
        {
            bool readFromCache = inputActionMap == null;
            switch (actionType)
            {
                case InputActionType.Value:
                    if (readFromCache)
                    {
                        if (!ActionStatesCache[playerId].GetValueActionState(id, out Vector2 value))
                        {
                            value = Vector2.zero;
                        }
                        return value;
                    }
                    return inputActionMap.FindAction(id).ReadValue<Vector2>();
                case InputActionType.Button:
                    if (!ActionStatesCache[playerId].GetButtonActionState(id, out ButtonState cachedButtonState))
                        cachedButtonState = ButtonState.Up;
                    if (readFromCache)
                    {
                        return cachedButtonState;
                    }
                    return ParseButtonState(inputActionMap, id, cachedButtonState);
                case InputActionType.PassThrough:
                    break;
                default:
                    break;
            }
            return null;
        }

        private static ButtonState ParseButtonState(InputActionMap map, string action, ButtonState cached)
        {
            bool flag = map.FindAction(action).ReadValue<float>() > 0.5f;
            if (cached == ButtonState.Consumed)
            {
                if (flag)
                {
                    return ButtonState.Consumed;
                }
                return ButtonState.Up;
            }
            if (flag)
            {
                if (cached != ButtonState.Held && cached != ButtonState.Pressed)
                {
                    return ButtonState.Pressed;
                }
                return ButtonState.Held;
            }
            if (cached != 0 && cached != ButtonState.Released)
            {
                return ButtonState.Released;
            }
            return ButtonState.Up;
        }
    }
}
