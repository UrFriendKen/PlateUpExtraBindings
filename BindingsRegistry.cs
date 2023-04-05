using Controllers;
using Kitchen;
using KitchenData;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace ExtraBindings
{
    public static class BindingsRegistry
    {
        public enum Category
        {
            Null,
            Movement,
            Interaction,
            Accessibility,
            Social,
            Menus
        }

        struct PlayersHashCache
        {
            int storedHash;

            public PlayersHashCache()
            {
                storedHash = GetHashCode();
            }

            public override int GetHashCode()
            {
                int hash = 17;
                if (Players.Main == null)
                    return hash;

                foreach (PlayerInfo player in Players.Main.All())
                {
                    hash = hash * 31 + player.ID.GetHashCode();
                }
                return hash;
            }

            public bool IsChanged(bool updateState = false)
            {
                int hash = GetHashCode();
                if (hash != storedHash)
                {
                    if (updateState)
                    {
                        storedHash = hash;
                    }
                    return true;
                }
                return false;
            }
        }

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

        [Serializable]
        private struct ProfileSaveData
        {
            public string ProfileName { get; private set; }

            public Dictionary<DeviceType, Dictionary<string, bool>> EnabledStates;

            public ProfileSaveData(string profileName)
            {
                ProfileName = profileName;
                EnabledStates = new Dictionary<DeviceType, Dictionary<string, bool>>();
            }

            public void Set(DeviceType peripheralType, string actionKey, bool value)
            {
                if (!EnabledStates.ContainsKey(peripheralType))
                {
                    EnabledStates.Add(peripheralType, new Dictionary<string, bool>());
                }

                if (!EnabledStates[peripheralType].ContainsKey(actionKey))
                {
                    EnabledStates[peripheralType].Add(actionKey, value);
                    return;
                }
                EnabledStates[peripheralType][actionKey] = value;
            }
        }

        static Dictionary<string, string> VanillaAdditionalLocalisations => new Dictionary<string, string>
        {
            { "REBIND_MOVEMENT", "Move" }
        };

        static Dictionary<string, InputAction> Registered = new Dictionary<string, InputAction>();

        static Dictionary<int, PlayerActionState> ActionStatesCache = new Dictionary<int, PlayerActionState>();

        static Dictionary<int, InputActionMap> InputActionMapCache = new Dictionary<int, InputActionMap>();

        static Dictionary<int, DeviceType> PlayerDeviceCache = new Dictionary<int, DeviceType>();

        private static Dictionary<Category, List<string>> ActionKeysByCategory;

        static bool isInit = false;

        static PlayersHashCache playersHashCache;

        const string SAVE_FOLDER = "ExtraBindings";
        const string SAVE_FILENAME = "ExtraBindings_SaveData";
        static Dictionary<string, ProfileSaveData> ProfileData = new Dictionary<string, ProfileSaveData>();

        private static void Init()
        {
            if (!isInit)
            {
                isInit = true;
                playersHashCache = new PlayersHashCache();
                ActionKeysByCategory = new Dictionary<Category, List<string>>();
                foreach (Category cat in Enum.GetValues(typeof(Category)).Cast<Category>())
                {
                    ActionKeysByCategory.Add(cat, new List<string>());
                }
                Main.LogInfo("Intialized ActionKeysByCategory.");
            }
        }

        public static Dictionary<Category, List<string>> GetActionKeysByCategory(bool includeDisallowedRebinds = true)
        {
            if (includeDisallowedRebinds)
            {
                return ActionKeysByCategory;
            }
            Dictionary<Category, List<string>> result = new Dictionary<Category, List<string>>();
            foreach(KeyValuePair<Category, List<string>> kvp in ActionKeysByCategory)
            {
                result.Add(kvp.Key, kvp.Value.Where(x => Registered[x].AllowRebind).ToList());
            }
            return result;
        }

        public static InputAction AddValueAction(string id, string displayName, Category category)
        {
            return AddAction(id, displayName, category, InputActionType.Value, allowRebind: false);
        }

        public static InputAction AddButtonAction(string id, string displayName, Category category, bool allowRebind = true)
        {
            return AddAction(id, displayName, category, InputActionType.Button, allowRebind);
        }

        private static InputAction AddAction(string id, string displayName, Category category, InputActionType inputType, bool allowRebind = true)
        {
            Init();
            if (category == Category.Null)
            {
                throw new ArgumentException("BindingsRegistry.Category.Null cannot be used!", "category");
            }
            InputAction action = AddAction(new InputAction(id, displayName, category, inputType, allowRebind));
            ActionKeysByCategory[category].Add(id);
            return action;
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

        public static bool ActionEnabled(int playerId, string action, bool? enabled = null, bool doNotSave = false)
        {
            if (!InputActionMapCache.ContainsKey(playerId))
            {
                Main.LogError($"Could not retrieve input map for {playerId}");
                return true;
            }

            UnityEngine.InputSystem.InputAction inputAction = InputActionMapCache[playerId].FindAction(action);
            return ActionEnabled(inputAction, enabled, doNotSave);
        }

        public static bool ActionEnabled(UnityEngine.InputSystem.InputAction action, bool? enabled = null, bool doNotSave = false)
        {
            if (enabled.HasValue)
            {
                switch (enabled.Value)
                {
                    case true:
                        action.Enable();
                        break;
                    case false:
                        action.Disable();
                        break;
                }
                if (!doNotSave)
                    SaveProfileData();
            }
            return action.enabled;
        }

        private static object GetPlayerActionState(int playerId, string action, InputActionType actionType)
        {
            if (!Registered.TryGetValue(action, out InputAction inputAction))
            {
                throw new ArgumentException($"{action} is not a registered actionKey!", "id");
            }
            if (inputAction.InputType != actionType)
            {
                string errorMsg = $"Wrong method used to retrieve actionKey state for InputActionType.{inputAction.InputType}! ";
                switch (actionType)
                {
                    case InputActionType.Value:
                        errorMsg += "Use GetPlayerValueActionState(int playerId, string actionKey) instead.";
                        break;
                    case InputActionType.Button:
                        errorMsg += "Use GetPlayerButtonActionState(int playerId, string actionKey) instead.";
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

        internal static void RegisterGlobalLocalisation()
        {
            Init();
            
            bool performed = false;
            foreach (KeyValuePair<string, string> kvp in VanillaAdditionalLocalisations)
            {
                if (!GameData.Main.GlobalLocalisation.Text.ContainsKey(kvp.Key))
                {
                    GameData.Main.GlobalLocalisation.Text.Add(kvp.Key, kvp.Value);
                    performed = true;
                }
            }

            foreach (KeyValuePair<string, InputAction> kvp in Registered)
            {
                if (!GameData.Main.GlobalLocalisation.Text.ContainsKey(kvp.Key))
                {
                    GameData.Main.GlobalLocalisation.Text.Add(kvp.Key, kvp.Value.Name);
                    performed = true;
                }
            }

            if (performed)
                Main.LogInfo($"Registered Global Localisation for controls");
        }

        internal static void AddActionsToMap(ref InputActionMap inputActionMap)
        {
            foreach (KeyValuePair<string, InputAction> binding in Registered)
            {
                inputActionMap.AddAction(binding.Key, binding.Value.InputType);
            }
        }

        internal static void SaveProfileData()
        {
            if (Players.Main == null)
                return;

            foreach (PlayerInfo player in Players.Main.All())
            {
                PlayerProfile profile = player.Profile;
                if (!profile.IsRealProfile)
                    continue;

                if (!InputActionMapCache.TryGetValue(player.ID, out InputActionMap map))
                {
                    Main.LogError($"Could not retrieve input map for {player.ID}");
                    continue;
                }

                if (!PlayerDeviceCache.TryGetValue(player.ID, out DeviceType peripheralType))
                {
                    Main.LogError($"Could not peripheral type for {player.ID}");
                    continue;
                }

                if (!ProfileData.ContainsKey(profile.Name))
                {
                    ProfileData.Add(profile.Name, new ProfileSaveData(profile.Name));
                }

                foreach (KeyValuePair<string, InputAction> action in Registered)
                {
                    ProfileData[profile.Name].Set(peripheralType, action.Key, ActionEnabled(player.ID, action.Value.ID));
                }
            }
            PruneDeletedProfiles();
            WriteProfileData(SAVE_FOLDER, SAVE_FILENAME);
        }

        private static void PruneDeletedProfiles()
        {
            IEnumerable<string> allProfiles = ProfileManager.Main.AllProfiles().Where(x => x.IsRealProfile).Select(x => x.Name);
            ProfileData = ProfileData.Where(kvp => allProfiles.Contains(kvp.Key)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        internal static void LoadEnabledStates()
        {
            ReadProfileData(SAVE_FOLDER, SAVE_FILENAME);
            foreach (PlayerInfo player in Players.Main.All())
            {
                PlayerProfile profile = player.Profile;
                if (!profile.IsRealProfile)
                    continue;

                if (!ProfileData.TryGetValue(profile.Name, out ProfileSaveData data))
                {
                    continue;
                }

                if (!InputActionMapCache.TryGetValue(player.ID, out InputActionMap map))
                {
                    Main.LogError($"Could not retrieve input map for {player.ID}");
                    continue;
                }

                if (!PlayerDeviceCache.TryGetValue(player.ID, out DeviceType peripheralType))
                {
                    Main.LogError($"Could not peripheral type for {player.ID}");
                    continue;
                }

                foreach (string actionKey in Registered.Keys)
                {
                    UnityEngine.InputSystem.InputAction inputAction = map.FindAction(actionKey);
                    if (inputAction == null)
                        continue;

                    if (!data.EnabledStates.TryGetValue(peripheralType, out Dictionary<string, bool> deviceDict))
                        continue;

                    if (!deviceDict.TryGetValue(actionKey, out bool enabled))
                        continue;

                    switch (enabled)
                    {
                        case true:
                            inputAction.Enable();
                            break;
                        case false:
                            inputAction.Disable();
                            break;
                    }
                }
            }
        }

        private static void WriteProfileData(string folder, string filename)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(JsonConvert.SerializeObject(ProfileData, Formatting.Indented));

            string fullSaveFolderPath = Path.Combine(Application.persistentDataPath, folder);
            if (!Directory.Exists(fullSaveFolderPath))
            {
                Directory.CreateDirectory(fullSaveFolderPath);
            }
            File.WriteAllText(Path.Combine(fullSaveFolderPath, filename + ".json"), sb.ToString());
            Main.LogInfo($"Saved Profile Data to {Path.Combine(fullSaveFolderPath, filename + ".json")}");
        }

        private static void ReadProfileData(string folder, string filename)
        {
            string fullSaveFolderPath = Path.Combine(Application.persistentDataPath, folder);
            string fullSaveFilePath = Path.Combine(fullSaveFolderPath, filename + ".json");
            if (!Directory.Exists(fullSaveFolderPath) || !File.Exists(fullSaveFilePath))
            {
                Main.LogWarning($"Save file does not exist, skipping. {fullSaveFilePath}");
                ProfileData = new Dictionary<string, ProfileSaveData>();
                return;
            }

            try
            {
                ProfileData = JsonConvert.DeserializeObject<Dictionary<string, ProfileSaveData>>(File.ReadAllText(Path.Combine(fullSaveFolderPath, filename + ".json")));
            }
            catch (Exception e)
            {
                Main.LogError($"Failed to parse json in {Path.Combine(fullSaveFolderPath, filename + ".json")}");
                Main.LogError(e.ToString());
                ProfileData = new Dictionary<string, ProfileSaveData>();
            }
        }

        internal static void ApplyKeyboardBindings(ref InputActionMap inputActionMap)
        {
            foreach (KeyValuePair<string, InputAction> binding in Registered)
            {
                binding.Value.ApplyBindings(inputActionMap.FindAction(binding.Key), DeviceType.Keyboard);
            }
        }

        internal static void ApplyGamepadBindings(ref InputActionMap inputActionMap)
        {
            foreach (KeyValuePair<string, InputAction> binding in Registered)
            {
                binding.Value.ApplyBindings(inputActionMap.FindAction(binding.Key), DeviceType.Controller);
            }
        }

        internal static void UpdateActionStates(int playerId, ControllerType controllerType, InputActionMap inputActionMap)
        {
            if (!ActionStatesCache.ContainsKey(playerId))
            {
                ActionStatesCache.Add(playerId, new PlayerActionState());
            }

            if (!InputActionMapCache.ContainsKey(playerId))
            {
                InputActionMapCache.Add(playerId, inputActionMap);
            }

            if (!PlayerDeviceCache.ContainsKey(playerId))
            {
                PlayerDeviceCache.Add(playerId, DeviceType.Controller);
            }
            PlayerDeviceCache[playerId] = InputAction.ControllerTypeMapping[controllerType];

            if (playersHashCache.IsChanged(true))
            {
                LoadEnabledStates();
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
