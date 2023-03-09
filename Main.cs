using Controllers;
using KitchenLib;
using KitchenLib.Event;
using KitchenMods;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

// Namespace should have "Kitchen" in the beginning
namespace ExtraBindings
{
    public class Main : BaseMod, IModSystem
    {
        // GUID must be unique and is recommended to be in reverse domain name notation
        // Mod Name is displayed to the player and listed in the mods menu
        // Mod Version must follow semver notation e.g. "1.2.3"
        public const string MOD_GUID = "IcedMilo.PlateUp.ExtraBindings";
        public const string MOD_NAME = "ExtraBindings";
        public const string MOD_VERSION = "0.1.0";
        public const string MOD_AUTHOR = "IcedMilo";
        public const string MOD_GAMEVERSION = ">=1.1.4";
        // Game version this mod is designed for in semver
        // e.g. ">=1.1.4" current and all future
        // e.g. ">=1.1.4 <=1.2.3" for all from/until

        // Boolean constant whose value depends on whether you built with DEBUG or RELEASE mode, useful for testing
#if DEBUG
        public const bool DEBUG_MODE = true;
#else
        public const bool DEBUG_MODE = false;
#endif

        public static AssetBundle Bundle;

        public Main() : base(MOD_GUID, MOD_NAME, MOD_AUTHOR, MOD_VERSION, MOD_GAMEVERSION, Assembly.GetExecutingAssembly()) { }

        protected override void OnInitialise()
        {
            LogWarning($"{MOD_GUID} v{MOD_VERSION} in use!");
        }

        private void AddGameData()
        {
            LogInfo("Attempting to register game data...");

            // AddGameDataObject<MyCustomGDO>();

            LogInfo("Done loading game data.");
        }

        protected override void OnUpdate()
        {
            foreach (KeyValuePair<int, ButtonState> state in BindingsRegistry.GetButtonActionStates("CustomButtonAction0"))
            {
                Main.LogInfo($"{state.Key}: {state.Value}");
                
            }
            foreach (KeyValuePair<int, Vector2> state in BindingsRegistry.GetValueActionStates("CustomValueAction"))
            {
                Main.LogInfo($"{state.Key}: {state.Value}");
            }
        }

        protected override void OnPostActivate(KitchenMods.Mod mod)
        {
            // TODO: Uncomment the following if you have an asset bundle.
            // TODO: Also, make sure to set EnableAssetBundleDeploy to 'true' in your ModName.csproj

            // LogInfo("Attempting to load asset bundle...");
            // Bundle = mod.GetPacks<AssetBundleModPack>().SelectMany(e => e.AssetBundles).First();
            // LogInfo("Done loading asset bundle.");

            // Register custom GDOs
            AddGameData();

            // Perform actions when game data is built
            Events.BuildGameDataEvent += delegate (object s, BuildGameDataEventArgs args)
            {
            };

            string[] keyboardDefaults = {
                KeyboardBinding.Button.Alpha1,
                KeyboardBinding.Button.Alpha2,
                KeyboardBinding.Button.Alpha3,
                KeyboardBinding.Button.Alpha4,
                KeyboardBinding.Button.Alpha5 };

            string[] controllerDefaults = {
                ControllerBinding.Button.DPadLeft,
                ControllerBinding.Button.DPadRight,
                ControllerBinding.Button.DPadUp,
                ControllerBinding.Button.DPadDown,
                ControllerBinding.Button.LeftStickButton };

            for (int i = 0; i < 5; i++)
            {
                string key = $"CustomButtonAction{i}";
                string displayText = $"Button{i}";
                BindingsRegistry.AddButtonAction(key, displayText)
                    .AddBinding(new KeyboardBinding(keyboardDefaults[i], isAnalog: false))
                    .AddBinding(new ControllerBinding(controllerDefaults[i], isAnalog: false));
            }

            BindingsRegistry.AddValueAction("CustomValueAction", "Value")
                .AddBinding(new KeyboardBinding(KeyboardBinding.Composite.WASD, isAnalog: false))
                .AddBinding(new ControllerBinding(ControllerBinding.Composite.LeftStick, isAnalog: true));
        }
        #region Logging
        public static void LogInfo(string _log) { Debug.Log($"[{MOD_NAME}] " + _log); }
        public static void LogWarning(string _log) { Debug.LogWarning($"[{MOD_NAME}] " + _log); }
        public static void LogError(string _log) { Debug.LogError($"[{MOD_NAME}] " + _log); }
        public static void LogInfo(object _log) { LogInfo(_log.ToString()); }
        public static void LogWarning(object _log) { LogWarning(_log.ToString()); }
        public static void LogError(object _log) { LogError(_log.ToString()); }
        #endregion
    }
}
