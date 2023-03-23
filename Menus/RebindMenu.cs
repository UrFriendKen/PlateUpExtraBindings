using Controllers;
using Kitchen.Modules;
using KitchenLib;
using UnityEngine;
using Kitchen;

namespace ExtraBindings.Menus
{
    internal class RebindMenu<T> : KLMenu<T>
    {
        int PlayerID;
        string Action;
        string LocalisationKey;

        public RebindMenu(Transform container, ModuleList module_list) : base(container, module_list)
        {
        }

        public override void Setup(int player_id)
        {
            PlayerID = player_id;

            AddLabel("Rebind Control");
            RemapElement remap = AddRemap(PlayerID, Action, LocalisationKey);
            AddButton("Unbind", delegate
            {
                BindingsRegistry.ActionEnabled(PlayerID, Action, false);
                remap.SetButton(PlayerID, Action);
            });
            New<SpacerElement>();
            New<SpacerElement>();
            AddButton(base.Localisation["MENU_BACK_SETTINGS"], delegate
            {
                RequestPreviousMenu();
            });
        }

        public void SetAction(string actionKey, string localisationKey)
        {
            Action = actionKey;
            LocalisationKey = localisationKey;
        }

        private RemapElement AddRemap(int playerId, string actionKey, string localisationKey, float scale = 1f, float padding = 0.2f)
        {
            RemapElement remapElement = New<RemapElement>();
            //remapElement.Position = position;
            remapElement.SetSize(DefaultElementSize.x * scale, DefaultElementSize.y * scale);
            remapElement.SetButton(playerId, actionKey);
            remapElement.SetLabel(Localisation[localisationKey]);
            remapElement.SetStyle(ElementStyle.RebindPrompt);
            //remapElement.SetSelectable(false);
            remapElement.OnActivate += delegate
            {
                StartRebind(playerId, localisationKey, actionKey, remapElement);
            };
            //ModuleList.AddModule(remapElement, position);
            return remapElement;
        }

        private void StartRebind(int player_id, string localisationKey, string actionKey, RemapElement remapElement)
        {
            remapElement.SetLabel(Localisation["REBIND_NOW"]);
            TriggerRebind(player_id, localisationKey, actionKey, remapElement);
        }

        private void EndRebind(string localisationKey, RemapElement remapElement)
        {
            remapElement.SetLabel(Localisation[localisationKey]);
        }

        private void TriggerRebind(int player_id, string localisationKey, string actionKey, RemapElement remapElement)
        {
            InputSourceIdentifier.DefaultInputSource.RequestRebinding(player_id, actionKey, delegate (RebindResult result)
            {
                switch (result)
                {
                    case RebindResult.Null:
                    case RebindResult.Cancelled:
                        BindingsRegistry.ActionEnabled(player_id, actionKey, BindingsRegistry.ActionEnabled(player_id, actionKey));
                        break;
                    case RebindResult.RejectedInUse:
                        remapElement.SetLabel(Localisation["REBIND_IN_USE"]);
                        return;
                    case RebindResult.Fail:
                        TriggerRebind(player_id, localisationKey, actionKey, remapElement);
                        return;
                    case RebindResult.Success:
                        remapElement.SetButton(player_id, actionKey);
                        BindingsRegistry.ActionEnabled(PlayerID, Action, true);
                        ProfileManager.Main.Save();
                        break;
                }
                EndRebind(localisationKey, remapElement);
            });
        }
    }
}
