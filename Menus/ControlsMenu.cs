using Controllers;
using Kitchen;
using Kitchen.Modules;
using KitchenData;
using KitchenLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ExtraBindings.BindingsRegistry;

namespace ExtraBindings.Menus
{
    internal class ControlsMenu<T> : KLMenu<T>
    {
        private static readonly float columnWidth = 3.5f;
        private static readonly float rowHeight = 0.3f;

        private static readonly float horizontalPadding = 0.1f;
        private static readonly float verticalPadding = 0.05f;

        private static readonly int maxColumns = 4;
        private static readonly int controlsPerColumn = 20;
        private static readonly int maxControlsPerPage = maxColumns * controlsPerColumn;

        private List<Category> categories;

        private List<string> categoriesStrings;

        private Option<Category> CategoryPageSelector;
        private Option<int> ControlPageSelector;

        // (LocalisationKey, ActionKey)
        private Dictionary<Category, List<(string, string)>> VanillaControls;
        private Dictionary<Category, List<(string, string)>> CustomControls;

        private RebindMenu<T> RebindMenu;

        public ControlsMenu(Transform container, ModuleList module_list, RebindMenu<T> rebindMenu) : base(container, module_list)
        {
            RebindMenu = rebindMenu;
            categories = Enum.GetValues(typeof(Category)).Cast<Category>().Where(x => x != Category.Null).ToList();
            categoriesStrings = Enum.GetNames(typeof(Category)).Where(x => x != Enum.GetName(typeof(Category), Category.Null)).ToList();

            VanillaControls = new Dictionary<Category, List<(string, string)>>();
            CustomControls = new Dictionary<Category, List<(string, string)>>();
            foreach (Category category in categories)
            {
                VanillaControls.Add(category, new List<(string, string)>());
                CustomControls.Add(category, new List<(string, string)>());
            }

            VanillaControls[Category.Movement] = new List<(string, string)> {
                ("REBIND_MOVEMENT", Controls.Movement),
                ("REBIND_HOLD_POSITION", Controls.StopMoving)
            };

            VanillaControls[Category.Interaction] = new List<(string, string)> {
                ("REBIND_INTERACT1", Controls.Interact1),
                ("REBIND_INTERACT2", Controls.Interact2),
                ("REBIND_INTERACT3", Controls.Interact3),
                ("REBIND_INTERACT4", Controls.Interact4),
            };
        }

        public override void Setup(int player_id)
        {
            BindingsRegistry.RegisterGlobalLocalisation();

            Dictionary<Category, List<string>> customControlActionKeys = GetActionKeysByCategory(includeDisallowedRebinds: false);
            foreach (Category category in categories)
            {
                CustomControls[category].Clear();
                for (int i = 0; i < customControlActionKeys[category].Count; i++)
                {
                    CustomControls[category].Add((customControlActionKeys[category][i], customControlActionKeys[category][i]));
                }
            }

            List<Category> usedCategories = new List<Category>();
            List<string> usedCategoriesStrings = new List<string>();

            int maxControlsPageIndex = 0;
            int maxControlsCount = 0;
            for (int i = 0; i < categories.Count; i++)
            {
                if (customControlActionKeys[categories[i]].Count > 0 || VanillaControls[categories[i]].Count > 0)
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
                usedCategories.Add(Category.Null);
                usedCategoriesStrings.Add("No Available Controls");
            }
            float headerPositionX = Mathf.Min(maxControlsCount/controlsPerColumn, maxColumns - 1) / 2f * (columnWidth + horizontalPadding);

            CategoryPageSelector = new Option<Category>(usedCategories, usedCategories[maxControlsPageIndex], usedCategoriesStrings);
            CategoryPageSelector.OnChanged += delegate (object _, Category result)
            {
                Redraw(player_id, result, headerPositionX);
            };
            Redraw(player_id, usedCategories[maxControlsPageIndex], headerPositionX);
        }

        private void Redraw(int player_id, Category category, float headerPositionX, int page = 0, bool isCategoryChanged = true)
        {
            ModuleList.Clear();

            Vector2 selectPosition = new Vector2(headerPositionX, 4f);
            Vector2 backButtonPosition = new Vector2(headerPositionX, 3.5f);
            Vector2 controlPageSelectPosition = new Vector2(headerPositionX, 3f);

            float topControlYPosition = 3f;
            for (int i = 0; i < 2; i++)
            {
                if (i == 0 && isCategoryChanged || i == 1 && !isCategoryChanged)
                {
                    SelectElement categorySelect = AddSelect(CategoryPageSelector, position: selectPosition);
                    continue;
                }

                int controlsCount = VanillaControls[category].Count + CustomControls[category].Count;
                int columns = controlsCount / controlsPerColumn;
                if (controlsCount > maxControlsPerPage)
                {
                    topControlYPosition = 2.5f;
                    List<int> controlPages = new List<int>();
                    List<string> controlPagesStrings = new List<string>();
                    for (int j = 0; j < controlsCount / maxControlsPerPage + 1; j++)
                    {
                        controlPages.Add(j);
                        controlPagesStrings.Add($"Page {j + 1}");
                    }
                    ControlPageSelector = new Option<int>(controlPages, page, controlPagesStrings);
                    ControlPageSelector.OnChanged += delegate (object _, int result)
                    {
                        Redraw(player_id, category, headerPositionX, result, isCategoryChanged: false);
                    };
                    SelectElement pageSelect = AddSelect(ControlPageSelector, position: controlPageSelectPosition);
                }
            }

            ButtonElement backButton = AddButton(base.Localisation["MENU_BACK_SETTINGS"], delegate
            {
                RequestPreviousMenu();
            }, position: backButtonPosition);

            if (category != Category.Null)
            {
                List<(string, string)> allControls = new List<(string, string)>();

                int vanillaControlsCount = VanillaControls[category].Count;
                allControls.AddRange(VanillaControls[category]);
                allControls.AddRange(CustomControls[category].OrderBy(x => x.Item2));

                int controlsToDraw = Mathf.Min(allControls.Count, maxControlsPerPage);
                int startIndex = page * maxControlsPerPage;
                for (int i = 0; i < controlsToDraw; i++)
                {
                    (string, string) control = allControls[startIndex + i];
                    string localisationKey = control.Item1;
                    string actionKey = control.Item2;

                    float columnIndex = Mathf.Floor(i / controlsPerColumn);
                    float rowIndex = i % controlsPerColumn;
                    Vector2 position = new Vector2(
                        x: columnIndex * (columnWidth + horizontalPadding),
                        y: rowIndex * -(rowHeight + verticalPadding) + topControlYPosition);
                    bool isVanilla = startIndex + i < vanillaControlsCount;
                    AddRemapMenuButton(player_id, localisationKey, actionKey, canBeUnbound: !isVanilla, position: position);
                }
            }
        }

        private ButtonElement AddButton(string label, Action<int> on_activate, int arg = 0, float scale = 1f, float padding = 0.2f, Vector2 position = default)
        {
            ButtonElement buttonElement = New<ButtonElement>(false);
            buttonElement.Position = position;
            buttonElement.SetSize(Math.Min(DefaultElementSize.x, columnWidth) * scale, DefaultElementSize.y * scale);
            buttonElement.SetLabel(label);
            buttonElement.SetStyle(Style);
            buttonElement.OnActivate += delegate
            {
                on_activate(arg);
            };
            ModuleList.AddModule(buttonElement, position);

            return buttonElement;
        }

        private SelectElement AddSelect<TOpt>(Option<TOpt> option, float scale = 1f, float padding = 0.2f, Vector2 position = default)
        {
            SelectElement selectElement = New<SelectElement>(false);
            selectElement.Position = position;
            selectElement.SetSize(Math.Min(DefaultElementSize.x, columnWidth) * scale, DefaultElementSize.y * scale);
            selectElement.SetOptions(option.Names);
            selectElement.SetStyle(Style);
            selectElement.Value = option.Chosen;
            selectElement.OnOptionHighlighted += option.SetChosen;
            ModuleList.AddModule(selectElement, position);
            return selectElement;
        }

        private RemapElement AddRemapMenuButton(int player_id, string localisationKey, string actionKey, bool canBeUnbound = false, float scale = 1f, float padding = 0.2f, Vector2 position = default, bool skip_stack = false)
        {
            RemapElement remapElement = New<RemapElement>(false);
            remapElement.Position = position;
            remapElement.SetSize(columnWidth * scale, rowHeight * scale);
            remapElement.SetButton(player_id, actionKey);
            remapElement.SetLabel(Localisation[localisationKey]);
            remapElement.SetStyle(ElementStyle.RebindPrompt);
            remapElement.OnActivate += delegate
            {
                RebindMenu.SetAction(actionKey, localisationKey, canBeUnbound);
                RequestSubMenu(typeof(RebindMenu<T>), skip_stack);
            };
            ModuleList.AddModule(remapElement, position);
            return remapElement;
        }
    }
}
