using Kitchen;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;

namespace ExtraBindings
{
    public abstract class Binding
    {
        public virtual string Name { get; protected set; }
        public virtual bool IsAnalog { get; protected set; }
        public abstract void ApplyTo(in UnityEngine.InputSystem.InputAction inputAction);
    }

    public class ButtonBinding : Binding
    {
        public readonly string Path;

        public ButtonBinding(string name, string path, bool isAnalog = false)
        {
            Name = name;
            Path = path;
            IsAnalog = isAnalog;
        }

        public override void ApplyTo(in UnityEngine.InputSystem.InputAction inputAction)
        {
            inputAction.AddBinding(Path);
        }
    }

    public abstract class CompositeBinding : Binding
    {
        public abstract HashSet<string> Parts { get; }

        public virtual List<(string, string)> PartPaths { get; } = new List<(string, string)>();

        protected bool IsAllPartsSet(out List<string> partsNotSet)
        {
            partsNotSet = new List<string>();
            IEnumerable<string> setPaths = PartPaths.Select(x => x.Item1);
            foreach (string part in Parts)
            {
                if (!setPaths.Contains(part))
                {
                    partsNotSet.Add(part);
                }
            }
            return partsNotSet.Count == 0;
        }

        public override void ApplyTo(in UnityEngine.InputSystem.InputAction inputAction)
        {
            if (!IsAllPartsSet(out List<string> partsNotSet))
            {
                Main.LogError($"Missing composite parts ({string.Join(", ", partsNotSet.ToArray())}) for {GetType()!}");
                return;
            }
            InputActionSetupExtensions.CompositeSyntax compositeSyntax = GetCompositeSyntax(inputAction);
            foreach ((string, string) path in PartPaths)
            {
                compositeSyntax.With(path.Item1, path.Item2);
            }
        }

        protected abstract InputActionSetupExtensions.CompositeSyntax GetCompositeSyntax(in UnityEngine.InputSystem.InputAction inputAction);

        public virtual CompositeBinding AddPart(string partName, string path)
        {
            partName = partName.ToLower();
            if (!Parts.Contains(partName))
            {
                Main.LogError($"{partName} is not a valid part for {GetType()}! Skipping.");
            }
            PartPaths.Add((partName, path));
            return this;
        }
    }

    public class AxisBinding : CompositeBinding
    {
        public enum Bias
        {
            None = 0,
            Positive = 1,
            Negative = 2
        }

        public virtual Bias WhichSideWins { get; protected set; }

        public virtual float MinValue { get; protected set; }

        public virtual float MaxValue { get; protected set; }

        public override HashSet<string> Parts => new HashSet<string>()
        {
            "positive", "negative"
        };

        public AxisBinding(Bias whichSideWins = Bias.None, float minValue = -1f, float maxValue = 1f)
        {
            WhichSideWins = whichSideWins;
            MinValue = minValue;
            MaxValue = maxValue;
            IsAnalog = true;
        }

        protected override InputActionSetupExtensions.CompositeSyntax GetCompositeSyntax(in UnityEngine.InputSystem.InputAction inputAction)
        {
            return inputAction.AddCompositeBinding($"Axis(whichSideWins = {(int)WhichSideWins}, minValue = {MinValue}, maxValue = {MaxValue})");
        }
    }

    public class Vector2DBinding : CompositeBinding
    {
        public enum Mode
        {
            DigitalNormalized = 0,
            Digital = 1,
            Analog = 2
        }

        public virtual Mode OutputMode { get; protected set; }

        public override HashSet<string> Parts => new HashSet<string>()
        {
            "up", "down", "left", "right"
        };

        public Vector2DBinding(Mode mode)
        {
            OutputMode = mode;
            IsAnalog = OutputMode == Mode.Analog;
        }

        protected override InputActionSetupExtensions.CompositeSyntax GetCompositeSyntax(in UnityEngine.InputSystem.InputAction inputAction)
        {
            return inputAction.AddCompositeBinding($"2DVector(mode = {(int)OutputMode})");
        }
    }

    public class ButtonOneModifierBinding : CompositeBinding
    {
        public override HashSet<string> Parts => new HashSet<string>()
        {
            "modifier", "button"
        };

        public ButtonOneModifierBinding(bool isAnalog = false)
        {
            IsAnalog = isAnalog;
        }

        protected override InputActionSetupExtensions.CompositeSyntax GetCompositeSyntax(in UnityEngine.InputSystem.InputAction inputAction)
        {
            return inputAction.AddCompositeBinding("ButtonWithOneModifier");
        }
    }

    public class ButtonTwoModifiersBinding : CompositeBinding
    {
        public override HashSet<string> Parts => new HashSet<string>()
        {
            "modifier1", "modifier2", "button"
        };

        public ButtonTwoModifiersBinding(bool isAnalog = false)
        {
            IsAnalog = isAnalog;
        }

        protected override InputActionSetupExtensions.CompositeSyntax GetCompositeSyntax(in UnityEngine.InputSystem.InputAction inputAction)
        {
            return inputAction.AddCompositeBinding("ButtonWithTwoModifiers");
        }
    }
}
