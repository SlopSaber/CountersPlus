using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using CountersPlus.UI.FlowCoordinators;
using HMUI;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace CountersPlus.UI.ViewControllers
{
    public class CountersPlusSettingSectionSelectionViewController : BSMLResourceViewController
    {
        [Inject] private CounterCatalog catalog { get; set; }
        [Inject] private LazyInject<CountersPlusSettingsFlowCoordinator> flowCoordinator { get; set; }
        [Inject] private LazyInject<CountersPlusCounterGridViewController> grid { get; set; }

        [UIComponent("list")] private CustomCellListTableData tableList { get; set; }
        [UIComponent("left-button")] private Button leftButton { get; set; }
        [UIComponent("right-button")] private Button rightButton { get; set; }
        [UIComponent("expand-button")] private Button expandButton { get; set; }

        private ScrollView scrollView;
        private bool expanded;

        public override string ResourceName => "CountersPlus.UI.BSML.SettingsSectionSelection.bsml";

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

            scrollView = tableList.TableView.scrollView;
            scrollView.scrollPositionChangedEvent += ScrollView_scrollPositionChangedEvent;

            if (addedToHierarchy)
            {
                catalog.ClearSelection();
                SetExpanded(false);
            }
            ReloadCells();
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
            scrollView.scrollPositionChangedEvent -= ScrollView_scrollPositionChangedEvent;
            if (removedFromHierarchy) expanded = false;
        }

        [UIAction("#post-parse")]
        private void Parsed()
        {
            var colors = rightButton.colors;
            colors.disabledColor = Color.black;
            rightButton.colors = colors;
            leftButton.colors = colors;
        }

        private void ReloadCells()
        {
            tableList.Data.Clear();
            for (int i = 0; i < catalog.Count; i++)
                tableList.Data.Add(catalog.CellInfo(i));
            tableList.TableView.ReloadData();

            if (catalog.SelectedIndex >= 0)
            {
                tableList.TableView.SelectCellWithIdx(catalog.SelectedIndex, false);
                tableList.TableView.ScrollToCellWithIdx(catalog.SelectedIndex, TableView.ScrollPositionType.Center, false);
            }
            else
            {
                tableList.TableView.ClearSelection();
                tableList.TableView.ScrollToCellWithIdx(0, TableView.ScrollPositionType.Beginning, false);
            }
            ScrollView_scrollPositionChangedEvent(0f);
        }

        private void ScrollView_scrollPositionChangedEvent(float _)
        {
            leftButton.interactable = scrollView._destinationPos > 0.001f;
            rightButton.interactable = scrollView._destinationPos < scrollView.contentSize - scrollView.scrollPageSize - 0.001f;
        }

        [UIAction("option-selected")]
        private void OptionSelected(TableView view, CountersPlusListTableCell cell) => catalog.Select(cell.CellIdx);

        [UIAction("toggle-grid")]
        private void ToggleGrid() => SetExpanded(!expanded);

        internal void SelectFromGrid(int idx)
        {
            SetExpanded(false);
            tableList.TableView.SelectCellWithIdx(idx, false);
            tableList.TableView.ScrollToCellWithIdx(idx, TableView.ScrollPositionType.Center, true);
            catalog.Select(idx);
        }

        private void SetExpanded(bool value)
        {
            expanded = value;
            expandButton.transform.localEulerAngles = new Vector3(0, 0, value ? 180 : 0);
            if (value)
            {
                grid.Value.SelectionHandler = SelectFromGrid;
                flowCoordinator.Value.PushToMainScreen(grid.Value);
            }
            else
            {
                flowCoordinator.Value.PopFromMainScreen();
            }
        }
    }
}
