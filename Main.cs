using Controllers;
using ExtraBindings.Menus;
using Kitchen;
using KitchenData;
using KitchenLib;
using KitchenLib.Event;
using KitchenMods;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

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

        public Main() : base(MOD_GUID, MOD_NAME, MOD_AUTHOR, MOD_VERSION, MOD_GAMEVERSION, Assembly.GetExecutingAssembly()) { }

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

        PlayersHashCache playersHashCache;

        protected override void OnInitialise()
        {
            LogWarning($"{MOD_GUID} v{MOD_VERSION} in use!");
            playersHashCache = new PlayersHashCache();
        }

        protected override void OnUpdate()
        {
            if (playersHashCache.IsChanged(true))
            {
                BindingsRegistry.LoadEnabledStates();
            }

            foreach (KeyValuePair<int, ButtonState> state in BindingsRegistry.GetButtonActionStates("CustomButtonAction0"))
            {
                if (state.Value == ButtonState.Pressed || state.Value == ButtonState.Released)
                    Main.LogInfo($"{state.Key}: \"CustomButtonAction0\" {state.Value}");
            }
        }

        protected override void OnPostActivate(KitchenMods.Mod mod)
        {
            SetupMenus();

            // Perform actions when game data is built
            Events.BuildGameDataEvent += delegate (object s, BuildGameDataEventArgs args)
            {
                if (args.firstBuild)
                {
                    // Will only be performed once
                }

                // Will be performed multiple times
                // See reply for when it happens
            };

            //string[] keyboardDefaults = {
            //    KeyboardBinding.Button.Alpha1,
            //    KeyboardBinding.Button.Alpha2,
            //    KeyboardBinding.Button.Alpha3,
            //    KeyboardBinding.Button.Alpha4,
            //    KeyboardBinding.Button.Alpha5 };

            //string[] controllerDefaults = {
            //    ControllerBinding.Button.DPadLeft,
            //    ControllerBinding.Button.DPadRight,
            //    ControllerBinding.Button.DPadUp,
            //    ControllerBinding.Button.DPadDown,
            //    ControllerBinding.Button.LeftStickButton };

            //BindingsRegistry.AddButtonAction($"Accessibility1", "Accessibility", BindingsRegistry.Category.Accessibility)
            //    .AddBinding(new KeyboardBinding(KeyboardBinding.Button.Alpha1, isAnalog: false))
            //    .AddBinding(new ControllerBinding(ControllerBinding.Button.DPadLeft, isAnalog: false));

            for (int i = 0; i < 81; i++)
            {
                string key = $"CustomButtonAction{i}";
                string displayText = $"Button{i}";
                BindingsRegistry.AddButtonAction(key, displayText, BindingsRegistry.Category.Interaction);
                    //.AddBinding(new KeyboardBinding(KeyboardBinding.Button.Alpha1, isAnalog: false))
                    //.AddBinding(new ControllerBinding(ControllerBinding.Button.DPadLeft, isAnalog: false));
            }

            //BindingsRegistry.AddValueAction("CustomValueAction", "Value", BindingsRegistry.Category.Movement)
            //    .AddBinding(new KeyboardBinding(KeyboardBinding.Composite.WASD, isAnalog: false))
            //    .AddBinding(new ControllerBinding(ControllerBinding.Composite.LeftStick, isAnalog: true));
        }

        private void SetupMenus()
        {

            Events.MainMenu_SetupEvent = (EventHandler<MainMenu_SetupArgs>)Delegate.Combine(Events.MainMenu_SetupEvent, (EventHandler<MainMenu_SetupArgs>)delegate (object s, MainMenu_SetupArgs args)
            {
                args.addSubmenuButton.Invoke(args.instance, new object[3]
                {
                    "Edit Controls",
                    typeof(ControlsMenu<PauseMenuAction>),
                    false
                });
            });
            Events.PlayerPauseView_SetupMenusEvent = (EventHandler<PlayerPauseView_SetupMenusArgs>)Delegate.Combine(Events.PlayerPauseView_SetupMenusEvent, (EventHandler<PlayerPauseView_SetupMenusArgs>)delegate (object s, PlayerPauseView_SetupMenusArgs args)
            {
                RebindMenu<PauseMenuAction> rebindMenu = new RebindMenu<PauseMenuAction>(args.instance.ButtonContainer, args.module_list);
                args.addMenu.Invoke(args.instance, new object[2]
                {
                    typeof(ControlsMenu<PauseMenuAction>),
                    new ControlsMenu<PauseMenuAction>(args.instance.ButtonContainer, args.module_list, rebindMenu)
                });


                args.addMenu.Invoke(args.instance, new object[2]
                {
                    typeof(RebindMenu<PauseMenuAction>),
                    rebindMenu
                });
            });
            //Events.PreferenceMenu_PauseMenu_CreateSubmenusEvent += (s, args) =>
            //{
            //    args.Menus.Add(typeof(ChangeControlsMenu<PauseMenuAction>), new ChangeControlsMenu<PauseMenuAction>(args.Container, args.Module_list));
            //};
            //ModsPreferencesMenu<PauseMenuAction>.RegisterMenu(MOD_NAME, typeof(ChangeControlsMenu<PauseMenuAction>), typeof(PauseMenuAction));
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
