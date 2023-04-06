using Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine.InputSystem;
using static ExtraBindings.BindingExtensions;

namespace ExtraBindings
{
    public enum DeviceType
    {
        Controller,
        Keyboard,
        Mouse,
        Touch,
        TouchButton
    }

    public abstract class DeviceMapping
    {
        public abstract ControllerType ControllerTypeFlags { get; }
        public abstract Dictionary<string, Binding> DeviceBindings { get; }

        public virtual void AddBinding(in UnityEngine.InputSystem.InputAction inputAction, string bindingKey)
        {
            if (!DeviceBindings.TryGetValue(bindingKey, out Binding binding))
            {
                throw new ArgumentException($"{bindingKey} is not a valid binding key for {GetType()}!", "bindingKey");
            }
            binding.ApplyTo(inputAction);
        }
    }

    public class ControllerMapping : DeviceMapping
    {
        public override ControllerType ControllerTypeFlags => ControllerType.Playstation | ControllerType.Xbox;

        protected Dictionary<string, Binding> _controllerBindings;

        public override Dictionary<string, Binding> DeviceBindings => _controllerBindings;

        public ControllerMapping()
        {
            _controllerBindings = typeof(ControllerButtons).GetFields()
                .ToDictionary(field => field.Name, field => new ButtonBinding(field.Name, (string)field.GetValue(null)) as Binding);
            _controllerBindings.Add(
                "LeftStick",
                new Vector2DBinding(Vector2DBinding.Mode.Analog)
                    .AddPart("Up", ControllerButtons.LeftStickUp)
                    .AddPart("Down", ControllerButtons.LeftStickDown)
                    .AddPart("Left", ControllerButtons.LeftStickLeft)
                    .AddPart("Right", ControllerButtons.LeftStickRight));
            _controllerBindings.Add(
                "RightStick",
                new Vector2DBinding(Vector2DBinding.Mode.Analog)
                    .AddPart("Up", ControllerButtons.RightStickUp)
                    .AddPart("Down", ControllerButtons.RightStickDown)
                    .AddPart("Left", ControllerButtons.RightStickLeft)
                    .AddPart("Right", ControllerButtons.RightStickRight));
            _controllerBindings.Add(
                "DPad",
                new Vector2DBinding(Vector2DBinding.Mode.DigitalNormalized)
                    .AddPart("Up", ControllerButtons.DPadUp)
                    .AddPart("Down", ControllerButtons.DPadDown)
                    .AddPart("Left", ControllerButtons.DPadLeft)
                    .AddPart("Right", ControllerButtons.DPadRight));
        }
    }

    public class KeyboardMapping : DeviceMapping
    {
        public override ControllerType ControllerTypeFlags => ControllerType.Keyboard;

        protected Dictionary<string, Binding> _keyboardBindings;

        public override Dictionary<string, Binding> DeviceBindings => _keyboardBindings;

        public KeyboardMapping()
        {
            _keyboardBindings = typeof(KeyboardButtons).GetFields()
                .ToDictionary(field => field.Name, field => new ButtonBinding(field.Name, (string)field.GetValue(null)) as Binding);
            _keyboardBindings.Add(
                "WASD",
                new Vector2DBinding(Vector2DBinding.Mode.DigitalNormalized)
                    .AddPart("Up", KeyboardButtons.W)
                    .AddPart("Down", KeyboardButtons.S)
                    .AddPart("Left", KeyboardButtons.A)
                    .AddPart("Right", KeyboardButtons.D));
            _keyboardBindings.Add(
                "Arrows",
                new Vector2DBinding(Vector2DBinding.Mode.DigitalNormalized)
                    .AddPart("Up", KeyboardButtons.UpArrow)
                    .AddPart("Down", KeyboardButtons.DownArrow)
                    .AddPart("Left", KeyboardButtons.LeftArrow)
                    .AddPart("Right", KeyboardButtons.RightArrow));
        }
    }

    public struct InputAction
    {
        public string ID;
        public string Name;
        public InputActionType InputType;
        public BindingsRegistry.Category Category;
        public bool AllowRebind;
        Dictionary<DeviceType, string> Bindings;

        static readonly Dictionary<DeviceType, DeviceMapping> Devices = new Dictionary<DeviceType, DeviceMapping>()
        {
            { DeviceType.Keyboard, new KeyboardMapping() },
            { DeviceType.Controller, new ControllerMapping() }
        };

        static readonly Dictionary<DeviceType, string> DefaultCompositeBinding = new Dictionary<DeviceType, string>()
        {
            { DeviceType.Keyboard, "WASD" },
            { DeviceType.Controller, "LeftStick" }
        };

        static readonly Dictionary<DeviceType, string> DefaultBinding = new Dictionary<DeviceType, string>()
        {
            { DeviceType.Keyboard, "Alpha0" },
            { DeviceType.Controller, "A" }
        };

        public static Dictionary<ControllerType, DeviceType> ControllerTypeMapping => new Dictionary<ControllerType, DeviceType>()
        {
            { ControllerType.Keyboard, DeviceType.Keyboard },
            { ControllerType.Playstation, DeviceType.Controller },
            { ControllerType.Xbox, DeviceType.Controller }
        };

        public InputAction(string id, string name, BindingsRegistry.Category category, InputActionType inputType, bool allowRebind)
        {
            ID = id;
            Name = name;
            InputType = inputType;
            Category = category;
            Bindings = new Dictionary<DeviceType, string>();
            AllowRebind = allowRebind;
        }

        public InputAction CopyInstance()
        {
            return new InputAction(ID, Name, Category, InputType, AllowRebind);
        }

        public bool IsDeviceBinded(DeviceType device)
        {
            return Bindings.ContainsKey(device);
        }

        public InputAction AddBinding(DeviceType device, string bindingKey)
        {
            if (!Bindings.ContainsKey(device))
            {
                Bindings.Add(device, bindingKey);
                Main.LogInfo($"Adding {device} binding for {ID}");
            }
            else
            {
                Bindings[device] = bindingKey;
                Main.LogWarning($"Overwriting {device} binding for {ID}");
            }
            return this;
        }

        internal void ApplyBindings(in UnityEngine.InputSystem.InputAction action, DeviceType device)
        {
            if (!Bindings.TryGetValue(device, out string deviceBinding) && action.bindings.Count == 0)
            {
                Main.LogInfo($"Applying default binding for {action.name}");
                ApplyDefaultBinding(action, device);
                action.Disable();
                return;
            }
            Devices[device].AddBinding(action, deviceBinding);
            action.Enable();
        }

        internal void ApplyBindings(in UnityEngine.InputSystem.InputActionMap inputActionMap, DeviceType device)
        {
            UnityEngine.InputSystem.InputAction action = inputActionMap.FindAction(ID);
            if (action == null)
            {
                Main.LogError($"InputActionMap does not contain action id {ID}!");
                return;
            }
            ApplyBindings(action, device);
        }

        public static void ApplyDefaultBinding(in UnityEngine.InputSystem.InputAction action, DeviceType device)
        {
            switch (action.type)
            {
                case InputActionType.Value:
                    Devices[device].AddBinding(action, DefaultCompositeBinding[device]);
                    break;
                case InputActionType.Button:
                    Devices[device].AddBinding(action, DefaultBinding[device]);
                    break;
                default:
                    break;
            }
        }

        public static void ApplyDefaultBinding(in UnityEngine.InputSystem.InputAction action, ControllerType device)
        {
            ApplyDefaultBinding(action, ControllerTypeMapping[device]);
        }
    }
}
