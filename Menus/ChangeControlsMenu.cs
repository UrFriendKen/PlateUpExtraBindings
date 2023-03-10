using Controllers;
using Kitchen;
using Kitchen.Modules;
using KitchenLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ExtraBindings.Menus
{
    internal class ChangeControlsMenu<T> : KLMenu<T>
    {
        private static readonly float columnWidth = 6f;

        private static readonly int controlsPerColumn = 25;

        private static readonly Vector2 selectPosition = new Vector2(1f, 4f);

        private static readonly Vector2 backButtonPosition = new Vector2(1f, 3.5f);

        private List<BindingsRegistry.Category> categories;

        private List<string> categoriesStrings;

        private Option<BindingsRegistry.Category> CategoryPageSelector;

        private Dictionary<BindingsRegistry.Category, List<string>> VanillaControls;
        private Dictionary<BindingsRegistry.Category, List<string>> CustomControls;

        public ChangeControlsMenu(Transform container, ModuleList module_list) : base(container, module_list)
        {
            categories = Enum.GetValues(typeof(BindingsRegistry.Category)).Cast<BindingsRegistry.Category>().Where(x => x != BindingsRegistry.Category.Null).ToList();
            categoriesStrings = Enum.GetNames(typeof(BindingsRegistry.Category)).Where(x => x != Enum.GetName(typeof(BindingsRegistry.Category), BindingsRegistry.Category.Null)).ToList();

            VanillaControls = new Dictionary<BindingsRegistry.Category, List<string>>();
            foreach (BindingsRegistry.Category category in categories)
            {
                VanillaControls.Add(category, new List<string>());
            }

            VanillaControls[BindingsRegistry.Category.Movement] = new List<string> {
                Controls.StopMoving
            };

            VanillaControls[BindingsRegistry.Category.Interaction] = new List<string> {
                Controls.Interact1,
                Controls.Interact2,
                Controls.Interact3,
                Controls.Interact4,
            };
        }

        public override void Setup(int player_id)
        {
            BindingsRegistry.RegisterGlobalLocalisation();
            BindingsRegistry.PlayerActionState playerbindings = BindingsRegistry.GetPlayerActionStates(player_id);
            CustomControls = BindingsRegistry.GetActionKeysByCategory(includeDisallowedRebinds: false);

            List<BindingsRegistry.Category> usedCategories = new List<BindingsRegistry.Category>();
            List<string> usedCategoriesStrings = new List<string>();

            for (int i = 0; i < categories.Count; i++)
            {
                if (CustomControls[categories[i]].Count > 0 || VanillaControls[categories[i]].Count > 0)
                {
                    usedCategories.Add(categories[i]);
                    usedCategoriesStrings.Add(categoriesStrings[i]);
                }
            }
            if (usedCategories.Count == 0)
            {
                usedCategories.Add(BindingsRegistry.Category.Null);
                usedCategoriesStrings.Add("No Available Controls");
            }
            CategoryPageSelector = new Option<BindingsRegistry.Category>(usedCategories, BindingsRegistry.Category.Movement, usedCategoriesStrings);
            CategoryPageSelector.OnChanged += delegate (object _, BindingsRegistry.Category result)
            {
                Redraw(player_id, result);
            };
            Redraw(player_id, usedCategories[0]);
        }

        private void Redraw(int player_id, BindingsRegistry.Category category)
        {
            ModuleList.Clear();
            AddSelect(CategoryPageSelector).Position = selectPosition;
            New<SpacerElement>();
            AddButton(base.Localisation["MENU_BACK_SETTINGS"], delegate
            {
                RequestPreviousMenu();
            }).Position = backButtonPosition;
            if (category != BindingsRegistry.Category.Null)
            {
                CreateRebindOptions(player_id, VanillaControls[category], CustomControls[category]);
            }
        }

        private void CreateRebindOptions(int player_id, List<string> vanillaControls, List<string> controls)
        {
            int columns = controls.Count / controlsPerColumn;
            int i = 0;

            List<string> allControls = new List<string>();

            allControls.AddRange(vanillaControls);
            allControls.AddRange(controls.OrderBy(x => x));

            allControls.OrderBy(x => x).ToList().ForEach(delegate (string action)
            {
                AddRebindOption(i, action, columns);
                i++;
            });
        }

        // To be changed to rebind element buttons
        private void AddRebindOption(int buttonIndex, string actionKey, int columns)
        {
            New<RemapElement>()
                .SetSize(columnWidth, 1f)
                .Position = new Vector2(Mathf.Floor(buttonIndex / controlsPerColumn) * columnWidth - (float)columns * columnWidth / 2f, (float)(buttonIndex % controlsPerColumn) * -0.25f + 3f);
            //InfoBoxElement infoBoxElement = AddInfo(actionKey);
            //infoBoxElement.SetSize(columnWidth, 1f);
            //infoBoxElement.Position = new Vector2(Mathf.Floor(buttonIndex / controlsPerColumn) * columnWidth - (float)columns * columnWidth / 2f, (float)(buttonIndex % controlsPerColumn) * -0.25f + 3f);
        }
    }
}
