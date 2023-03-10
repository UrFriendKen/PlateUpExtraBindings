// Kitchen.Modules.ControlRebindElement
using Controllers;
using Kitchen;
using Kitchen.Modules;
using KitchenData;
using UnityEngine;

public class CustomControlRebindElement : Element
{
    [Header("References")]
    [SerializeField]
    private PanelElement Panel;

    [Header("State")]
    private LabelElement RebindMessage;

    private ModuleList ModuleList;

    private int PlayerID;

    private bool IsDisplayOnly;

    private GlobalLocalisation Localisation => GameData.Main.GlobalLocalisation;

    private ControllerIcons Icons => GameData.Main.GlobalLocalisation.ControllerIcons;

    private IInputSource InputSource => InputSourceIdentifier.DefaultInputSource;

    public override Bounds BoundingBox => ModuleList.BoundingBox;

    public override bool IsSelectable => !IsDisplayOnly;

    public void Setup(int player, bool display_only, bool show_panel = false, ModuleList use_module_list = null)
    {
        if (use_module_list != null)
        {
            if (ModuleList != null)
            {
                ModuleList.Destroy();
            }
            ModuleList = use_module_list;
        }
        else if (ModuleList != null)
        {
            ModuleList.Clear();
        }
        else
        {
            ModuleList = new ModuleList();
        }
        PlayerID = player;
        IsDisplayOnly = display_only;
        AddRebindOption("REBIND_INTERACT1", Controls.Interact1);
        AddRebindOption("REBIND_INTERACT2", Controls.Interact2);
        AddRebindOption("REBIND_INTERACT3", Controls.Interact3);
        AddRebindOption("REBIND_INTERACT4", Controls.Interact4);
        AddRebindOption("REBIND_HOLD_POSITION", Controls.StopMoving);
        Panel.SetTarget(ModuleList);
        Panel.gameObject.SetActive(show_panel);
        RebindMessage = Add<TextElement>();
        RebindMessage.gameObject.SetActive(value: false);
    }

    public void AddRebindOption(string localisation_key, string control)
    {
        RemapElement remapElement = Add<RemapElement>();
        remapElement.SetStyle(ElementStyle.RebindPrompt);
        remapElement.SetSize(2f, 0.3f);
        remapElement.SetLabel(Localisation[localisation_key]);
        remapElement.SetButton(PlayerID, control);
        remapElement.OnActivate += delegate
        {
            StartRebind(control);
        };
        ModuleList.AddModule(remapElement);
        remapElement.LoseFocus();
    }

    private void StartRebind(string action)
    {
        foreach (ModuleInstance module in ModuleList.Modules)
        {
            if (module.Module is RemapElement remapElement)
            {
                remapElement.gameObject.SetActive(value: false);
            }
        }
        RebindMessage.gameObject.SetActive(value: true);
        RebindMessage.SetLabel(Localisation["REBIND_NOW"]);
        if (Panel.isActiveAndEnabled)
        {
            Panel.SetTarget(RebindMessage);
        }
        TriggerRebind(action);
    }

    private void EndRebind()
    {
        foreach (ModuleInstance module in ModuleList.Modules)
        {
            if (module.Module is RemapElement remapElement)
            {
                remapElement.gameObject.SetActive(value: true);
            }
        }
        RebindMessage.gameObject.SetActive(value: false);
        if (Panel.isActiveAndEnabled)
        {
            Panel.SetTarget(ModuleList);
        }
    }

    private void TriggerRebind(string action)
    {
        InputSource.RequestRebinding(PlayerID, action, delegate (RebindResult result)
        {
            switch (result)
            {
                case RebindResult.RejectedInUse:
                    RebindMessage.SetLabel(Localisation["REBIND_IN_USE"]);
                    return;
                case RebindResult.Fail:
                    TriggerRebind(action);
                    return;
                case RebindResult.Success:
                    ProfileManager.Main.Save();
                    break;
            }
            EndRebind();
        });
    }

    public override bool HandleInteraction(InputState state)
    {
        return ModuleList.HandleInteraction(state);
    }

    public override void Destroy()
    {
        ModuleList.Destroy();
    }
}
