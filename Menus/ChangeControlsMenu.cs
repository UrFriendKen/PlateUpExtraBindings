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
        private static readonly float columnWidth = 4f;
        private static readonly float rowHeight = 0.3f;

        private static readonly int controlsPerColumn = 20;

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

            int maxControlsPageIndex = 0;
            int maxControlsCount = 0;
            for (int i = 0; i < categories.Count; i++)
            {
                if (CustomControls[categories[i]].Count > 0 || VanillaControls[categories[i]].Count > 0)
                {
                    usedCategories.Add(categories[i]);
                    usedCategoriesStrings.Add(categoriesStrings[i]);
                    int count = CustomControls[categories[i]].Count + VanillaControls[categories[i]].Count;
                    if (count > maxControlsCount)
                    {
                        maxControlsCount = count;
                        maxControlsPageIndex = usedCategories.Count - 1;
                    }
                }
            }
            if (usedCategories.Count == 0)
            {
                usedCategories.Add(BindingsRegistry.Category.Null);
                usedCategoriesStrings.Add("No Available Controls");
            }
            float headerPostionX = maxControlsCount/controlsPerColumn / 2f * columnWidth;

            CategoryPageSelector = new Option<BindingsRegistry.Category>(usedCategories, usedCategories[maxControlsPageIndex], usedCategoriesStrings);
            CategoryPageSelector.OnChanged += delegate (object _, BindingsRegistry.Category result)
            {
                Redraw(player_id, result, headerPostionX);
            };
            Redraw(player_id, usedCategories[maxControlsPageIndex], headerPostionX);
        }

        private void Redraw(int player_id, BindingsRegistry.Category category, float headerPositionX)
        {
            ModuleList.Clear();

            Vector2 selectPosition = new Vector2(headerPositionX, 4f);
            Vector2 backButtonPosition = new Vector2(headerPositionX, 3.5f);
            AddSelect(CategoryPageSelector, selectPosition);
            New<SpacerElement>();
            ButtonElement backButton = AddButton(base.Localisation["MENU_BACK_SETTINGS"], delegate
            {
                RequestPreviousMenu();
            }, position: backButtonPosition);

            int controlsCount = VanillaControls[category].Count + CustomControls[category].Count;
            int columns = controlsCount / controlsPerColumn;

            if (category != BindingsRegistry.Category.Null)
            {
                int i = 0;

                List<string> allControls = new List<string>();

                allControls.AddRange(VanillaControls[category]);
                allControls.AddRange(CustomControls[category].OrderBy(x => x));

                allControls.OrderBy(x => x).ToList().ForEach(delegate (string action)
                {
                    float columnIndex = Mathf.Floor(i / controlsPerColumn);
                    float rowIndex = i % controlsPerColumn;
                    Vector2 position = new Vector2(
                        x: columnIndex * columnWidth,
                        y: rowIndex * -rowHeight + 3f);
                    AddRebindOption(player_id, action, position);
                    i++;
                });
            }
        }

        private ButtonElement AddButton(string label, Action<int> on_activate, int arg = 0, float scale = 1f, float padding = 0.2f, Vector2 position = default)
        {
            ButtonElement buttonElement = New<ButtonElement>(false);
            buttonElement.Position = position;
            buttonElement.SetSize(columnWidth * scale, rowHeight * scale);
            buttonElement.SetLabel(label);
            buttonElement.SetStyle(Style);
            buttonElement.OnActivate += delegate
            {
                on_activate(arg);
            };
            ModuleList.AddModule(buttonElement, position);

            return buttonElement;
        }

        private SelectElement AddSelect<TOpt>(Option<TOpt> option, Vector2 position = default)
        {
            SelectElement selectElement = New<SelectElement>(false);
            selectElement.Position = position;
            selectElement.SetSize(DefaultElementSize.x, DefaultElementSize.y);
            selectElement.SetOptions(option.Names);
            selectElement.SetStyle(Style);
            selectElement.Value = option.Chosen;
            selectElement.OnOptionHighlighted += option.SetChosen;
            ModuleList.AddModule(selectElement, position);
            return selectElement;
        }


        // To be changed to rebind element buttons
        private Element AddRebindOption(int player_id, string actionKey, Vector2 position)
        {
            return AddButton(actionKey, null, position: position);
        }
    }
}
