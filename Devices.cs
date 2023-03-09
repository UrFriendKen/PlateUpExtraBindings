﻿using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace ExtraBindings
{
    public enum PeripheralType
    {
        Controller,
        Keyboard,
        Mouse,
        Touch,
        TouchButton
    }

    public abstract class Binding
    {
        public class Control { }

        public PeripheralType Device { get; protected set; }
        public bool IsAnalog { get; set; } = false;
        public string Path { get; set; } = null;

        public void ApplyTo(UnityEngine.InputSystem.InputAction inputAction)
        {
            if (inputAction.type == InputActionType.Value)
            {
                AddCompositeBinding(inputAction);
            }
            else
            {
                AddBinding(inputAction);
            }
        }

        protected abstract void AddBinding(in UnityEngine.InputSystem.InputAction inputAction);
        protected abstract void AddCompositeBinding(in UnityEngine.InputSystem.InputAction inputAction);
    }

    public class ControllerBinding : Binding
    {
        public class Button : Control
        {
            public const string A = "<Gamepad>/buttonSouth";
            public const string B = "<Gamepad>/buttonWest";
            public const string X = "<Gamepad>/buttonEast";
            public const string Y = "<Gamepad>/buttonNorth";
            public const string LeftShoulder = "<Gamepad>/leftShoulder";
            public const string RightShoulder = "<Gamepad>/rightShoulder";
            public const string LeftTrigger = "<Gamepad>/leftTrigger";
            public const string RightTrigger = "<Gamepad>/rightTrigger";
            public const string Select = "<Gamepad>/select";
            public const string Start = "<Gamepad>/start";
            public const string LeftStickUp = "<Gamepad>/leftStick/up";
            public const string LeftStickDown = "<Gamepad>/leftStick/down";
            public const string LeftStickLeft = "<Gamepad>/leftStick/left";
            public const string LeftStickRight = "<Gamepad>/leftStick/right";
            public const string LeftStickButton = "<Gamepad>/leftStick/button";
            public const string RightStickUp = "<Gamepad>/rightStick/up";
            public const string RightStickDown = "<Gamepad>/rightStick/down";
            public const string RightStickLeft = "<Gamepad>/rightStick/left";
            public const string RightStickRight = "<Gamepad>/rightStick/right";
            public const string RightStickButton = "<Gamepad>/rightStick/button";
            public const string DPadUp = "<Gamepad>/dpad/up";
            public const string DPadDown = "<Gamepad>/dpad/down";
            public const string DPadLeft = "<Gamepad>/dpad/left";
            public const string DPadRight = "<Gamepad>/dpad/right";
        }

        public class Composite : Control
        {
            public const string LeftStick = "<Gamepad>/leftStick";
            public const string RightStick = "<Gamepad>/rightStick";
            public const string DPad = "<Gamepad>/dpad";

        }

        public ControllerBinding(string path, bool isAnalog = true)
        {
            this.Path = path;
            this.IsAnalog = isAnalog;
            this.Device = PeripheralType.Controller;
        }

        protected override void AddBinding(in UnityEngine.InputSystem.InputAction inputAction)
        {
            if (Path != null)
            {
                inputAction.AddBinding(Path);
            }
        }

        protected override void AddCompositeBinding(in UnityEngine.InputSystem.InputAction inputAction)
        {
            string up;
            string down;
            string left;
            string right;

            switch (Path)
            {
                case Composite.LeftStick:
                    up = Button.LeftStickUp;
                    down = Button.LeftStickDown;
                    left = Button.LeftStickLeft;
                    right = Button.LeftStickRight;
                    break;
                case Composite.RightStick:
                    up = Button.RightStickUp;
                    down = Button.RightStickDown;
                    left = Button.RightStickLeft;
                    right = Button.RightStickRight;
                    break;
                case Composite.DPad:
                    up = Button.DPadUp;
                    down = Button.DPadDown;
                    left = Button.DPadLeft;
                    right = Button.DPadRight;
                    break;
                default:
                    Main.LogInfo("Invalid composite");
                    return;
            }

            inputAction.AddCompositeBinding(IsAnalog ? "2DVector(mode=2)" : "2DVector")
                .With("Up", up)
                .With("Down", down)
                .With("Left", left)
                .With("Right", right);
        }
    }

    public class KeyboardBinding : Binding
    {
        public class Button : Control
        {
            public const string A = "<Keyboard>/a";
            public const string B = "<Keyboard>/b";
            public const string C = "<Keyboard>/c";
            public const string D = "<Keyboard>/d";
            public const string E = "<Keyboard>/e";
            public const string F = "<Keyboard>/f";
            public const string G = "<Keyboard>/g";
            public const string H = "<Keyboard>/h";
            public const string I = "<Keyboard>/i";
            public const string J = "<Keyboard>/j";
            public const string K = "<Keyboard>/k";
            public const string L = "<Keyboard>/l";
            public const string M = "<Keyboard>/m";
            public const string N = "<Keyboard>/n";
            public const string O = "<Keyboard>/o";
            public const string P = "<Keyboard>/p";
            public const string Q = "<Keyboard>/q";
            public const string R = "<Keyboard>/r";
            public const string S = "<Keyboard>/s";
            public const string T = "<Keyboard>/t";
            public const string U = "<Keyboard>/u";
            public const string V = "<Keyboard>/v";
            public const string W = "<Keyboard>/w";
            public const string X = "<Keyboard>/x";
            public const string Y = "<Keyboard>/y";
            public const string Z = "<Keyboard>/z";
            public const string Alpha0 = "<Keyboard>/0";
            public const string Alpha1 = "<Keyboard>/1";
            public const string Alpha2 = "<Keyboard>/2";
            public const string Alpha3 = "<Keyboard>/3";
            public const string Alpha4 = "<Keyboard>/4";
            public const string Alpha5 = "<Keyboard>/5";
            public const string Alpha6 = "<Keyboard>/6";
            public const string Alpha7 = "<Keyboard>/7";
            public const string Alpha8 = "<Keyboard>/8";
            public const string Alpha9 = "<Keyboard>/9";
            public const string Numpad0 = "<Keyboard>/numpad0";
            public const string Numpad1 = "<Keyboard>/numpad1";
            public const string Numpad2 = "<Keyboard>/numpad2";
            public const string Numpad3 = "<Keyboard>/numpad3";
            public const string Numpad4 = "<Keyboard>/numpad4";
            public const string Numpad5 = "<Keyboard>/numpad5";
            public const string Numpad6 = "<Keyboard>/numpad6";
            public const string Numpad7 = "<Keyboard>/numpad7";
            public const string Numpad8 = "<Keyboard>/numpad8";
            public const string Numpad9 = "<Keyboard>/numpad9";
            public const string F1 = "<Keyboard>/f1";
            public const string F2 = "<Keyboard>/f2";
            public const string F3 = "<Keyboard>/f3";
            public const string F4 = "<Keyboard>/f4";
            public const string F5 = "<Keyboard>/f5";
            public const string F6 = "<Keyboard>/f6";
            public const string F7 = "<Keyboard>/f7";
            public const string F8 = "<Keyboard>/f8";
            public const string F9 = "<Keyboard>/f9";
            public const string F10 = "<Keyboard>/f10";
            public const string F11 = "<Keyboard>/f11";
            public const string F12 = "<Keyboard>/f12";
            public const string Backspace = "<Keyboard>/backspace";
            public const string Tab = "<Keyboard>/tab";
            public const string Clear = "<Keyboard>/clear";
            public const string Return = "<Keyboard>/enter";
            public const string Pause = "<Keyboard>/pause";
            public const string Escape = "<Keyboard>/escape";
            public const string Space = "<Keyboard>/space";
            public const string Delete = "<Keyboard>/delete";
            public const string Insert = "<Keyboard>/insert";
            public const string Home = "<Keyboard>/home";
            public const string End = "<Keyboard>/end";
            public const string PageUp = "<Keyboard>/pageUp";
            public const string PageDown = "<Keyboard>/pageDown";
            public const string LeftArrow = "<Keyboard>/leftArrow";
            public const string RightArrow = "<Keyboard>/rightArrow";
            public const string UpArrow = "<Keyboard>/upArrow";
            public const string DownArrow = "<Keyboard>/downArrow";
            public const string NumLock = "<Keyboard>/numLock";
            public const string CapsLock = "<Keyboard>/capsLock";
            public const string ScrollLock = "<Keyboard>/scrollLock";
            public const string RightShift = "<Keyboard>/rightShift";
            public const string LeftShift = "<Keyboard>/leftShift";
            public const string RightAlt = "<Keyboard>/rightAlt";
            public const string LeftAlt = "<Keyboard>/leftAlt";
            public const string RightControl = "<Keyboard>/rightCtrl";
            public const string LeftControl = "<Keyboard>/leftCtrl";
            public const string SemiColon = "<Keyboard>/semicolon";
            public const string Equal = "<Keyboard>/equals";
            public const string Comma = "<Keyboard>/comma";
            public const string Underscore = "<Keyboard>/underscore";
            public const string Period = "<Keyboard>/period";
            public const string Slash = "<Keyboard>/slash";
            public const string BackQuote = "<Keyboard>/backquote";
            public const string LeftBracket = "<Keyboard>/leftBracket";
            public const string Backslash = "<Keyboard>/backslash";
            public const string RightBracket = "<Keyboard>/rightBracket";
            public const string Quote = "<Keyboard>/quote";
        }

        public class Composite : Control
        {
            public const string WASD = "<Keyboard>/wasd";
            public const string Arrows = "<Keyboard>/arrows";

        }

        public KeyboardBinding(string path, bool isAnalog = false)
        {
            this.Path = path;
            this.IsAnalog = isAnalog;
            this.Device = PeripheralType.Keyboard;
        }

        protected override void AddBinding(in UnityEngine.InputSystem.InputAction inputAction)
        {
            if (Path != null)
            {
                inputAction.AddBinding(Path);
            }
        }

        protected override void AddCompositeBinding(in UnityEngine.InputSystem.InputAction inputAction)
        {
            string up;
            string down;
            string left;
            string right;

            switch (Path)
            {
                case Composite.WASD:
                    up = Button.W;
                    down = Button.S;
                    left = Button.A;
                    right = Button.D;
                    break;
                case Composite.Arrows:
                    up = Button.UpArrow;
                    down = Button.DownArrow;
                    left = Button.LeftArrow;
                    right = Button.RightArrow;
                    break;
                default:
                    Main.LogInfo("Invalid composite");
                    return;
            }

            inputAction.AddCompositeBinding(IsAnalog ? "2DVector(mode=2)" : "2DVector")
                .With("Up", up)
                .With("Down", down)
                .With("Left", left)
                .With("Right", right);
        }
    }

    public struct InputAction
    {
        public string ID;
        public string Name;
        public InputActionType InputType;
        List<Binding> Bindings;

        public InputAction(string id, string name, InputActionType inputType)
        {
            ID = id;
            Name = name;
            InputType = inputType;
            Bindings = new List<Binding>();
        }

        public InputAction AddBinding(Binding binding)
        {
            Bindings.Add(binding);
            return this;
        }

        internal void ApplyBindings(in UnityEngine.InputSystem.InputAction action, PeripheralType device)
        {
            foreach (Binding binding in Bindings)
            {
                if (device == binding.Device)
                {
                    binding.ApplyTo(action);
                }
            }
        }
    }
}
