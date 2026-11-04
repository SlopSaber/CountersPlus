using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace CountersPlus.UI.ViewControllers
{
    public class CountersPlusSettingSectionSelectionViewController : BSMLResourceViewController
    {
        private const int FlyoutColumns = 5;
        private const float FlyoutCellSize = 20f;
        private const float FlyoutSpacing = 2f;
        private const float FlyoutPadding = 2f;
        private const float FlyoutAnimationSeconds = 0.15f;
        private static readonly Color SelectedColor = new Color(0f, 0.75f, 1f);

        [Inject] private CounterCatalog catalog { get; set; }

        [UIComponent("list")] private CustomCellListTableData tableList { get; set; }
        [UIComponent("left-button")] private Button leftButton { get; set; }
        [UIComponent("right-button")] private Button rightButton { get; set; }
        [UIComponent("expand-button")] private Button expandButton { get; set; }
        [UIObject("flyout")] private GameObject flyout { get; set; }
        [UIComponent("flyout-grid")] private GridLayoutGroup flyoutGrid { get; set; }

        private readonly List<FlyoutCell> flyoutCells = new List<FlyoutCell>();
        private ScrollView scrollView;
        private bool expanded;
        private Coroutine animation;

        public override string ResourceName => "CountersPlus.UI.BSML.SettingsSectionSelection.bsml";

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

            scrollView = tableList.TableView.scrollView;
            scrollView.scrollPositionChangedEvent += ScrollView_scrollPositionChangedEvent;

            if (firstActivation)
            {
                flyoutGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                flyoutGrid.constraintCount = FlyoutColumns;
                flyout.AddComponent<RectMask2D>();
            }
            if (addedToHierarchy) catalog.ClearSelection();

            SetExpanded(false, false);
            ReloadCells();
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
            scrollView.scrollPositionChangedEvent -= ScrollView_scrollPositionChangedEvent;
            SetExpanded(false, false);
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
            var items = new List<CustomListTableData.CustomCellInfo>();
            for (int i = 0; i < catalog.Count; i++)
                items.Add(catalog.CellInfo(i));
            tableList.Data = items;
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
            RebuildFlyout(items);
        }

        private void RebuildFlyout(List<CustomListTableData.CustomCellInfo> items)
        {
            foreach (FlyoutCell cell in flyoutCells)
                if (cell.Icon != null) Destroy(cell.Icon.gameObject);
            flyoutCells.Clear();

            for (int i = 0; i < items.Count; i++)
            {
                var cell = new FlyoutCell(i, this);
                string hint = System.Security.SecurityElement.Escape(items[i].Text ?? string.Empty);
                BSMLParser.Instance.Parse($"<clickable-image id='icon' preserve-aspect='true' on-click='clicked' hover-hint='{hint}'/>",
                    flyoutGrid.gameObject, cell);
                cell.Apply((CountersPlusListTableCell)items[i]);
                flyoutCells.Add(cell);
            }
            RefreshFlyoutSelection();
        }

        private void RefreshFlyoutSelection()
        {
            foreach (FlyoutCell cell in flyoutCells)
                cell.SetSelected(cell.Index == catalog.SelectedIndex);
        }

        private void ScrollView_scrollPositionChangedEvent(float _)
        {
            leftButton.interactable = scrollView._destinationPos > 0.001f;
            rightButton.interactable = scrollView._destinationPos < scrollView.contentSize - scrollView.scrollPageSize - 0.001f;
        }

        [UIAction("option-selected")]
        private void OptionSelected(TableView view, CountersPlusListTableCell cell)
        {
            catalog.Select(cell.CellIdx);
            RefreshFlyoutSelection();
        }

        [UIAction("toggle-flyout")]
        private void ToggleFlyout() => SetExpanded(!expanded, true);

        private void SelectFromFlyout(int idx)
        {
            tableList.TableView.SelectCellWithIdx(idx, false);
            tableList.TableView.ScrollToCellWithIdx(idx, TableView.ScrollPositionType.Center, false);
            catalog.Select(idx);
            RefreshFlyoutSelection();
            SetExpanded(false, true);
        }

        private float ExpandedHeight()
        {
            int rows = Mathf.Max(1, Mathf.CeilToInt(flyoutCells.Count / (float)FlyoutColumns));
            return rows * FlyoutCellSize + (rows - 1) * FlyoutSpacing + FlyoutPadding * 2;
        }

        private void SetExpanded(bool value, bool animated)
        {
            expanded = value;
            if (expandButton != null)
                expandButton.transform.localEulerAngles = new Vector3(0, 0, value ? 180 : 0);
            if (flyout == null) return;

            if (animation != null)
            {
                StopCoroutine(animation);
                animation = null;
            }

            var rect = (RectTransform)flyout.transform;
            float collapsed = FlyoutCellSize + FlyoutPadding * 2;
            float target = value ? ExpandedHeight() : collapsed;
            if (!animated || !isActiveAndEnabled)
            {
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, target);
                flyout.SetActive(value);
                return;
            }

            flyout.SetActive(true);
            animation = StartCoroutine(AnimateFlyout(rect, value ? collapsed : rect.sizeDelta.y, target, value));
        }

        private IEnumerator AnimateFlyout(RectTransform rect, float from, float to, bool stayOpen)
        {
            for (float t = 0; t < FlyoutAnimationSeconds; t += Time.deltaTime)
            {
                float eased = 1f - Mathf.Pow(1f - t / FlyoutAnimationSeconds, 3f);
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, Mathf.Lerp(from, to, eased));
                yield return null;
            }
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, to);
            flyout.SetActive(stayOpen);
            animation = null;
        }

        private class FlyoutCell
        {
            private readonly CountersPlusSettingSectionSelectionViewController owner;
            private Color baseColor = Color.white;

            [UIComponent("icon")] public ClickableImage Icon = null;

            public int Index { get; }

            public FlyoutCell(int index, CountersPlusSettingSectionSelectionViewController owner)
            {
                Index = index;
                this.owner = owner;
            }

            public void Apply(CountersPlusListTableCell info)
            {
                Icon.sprite = info.Icon;
                baseColor = info.IsAccent ? CountersPlusListTableCell.AccentColor : Color.white;
            }

            public void SetSelected(bool selected)
            {
                Color color = selected ? SelectedColor : baseColor;
                Icon.DefaultColor = color;
                Icon.HighlightColor = Color.Lerp(color, Color.white, 0.5f);
            }

            [UIAction("clicked")]
            private void Clicked() => owner.SelectFromFlyout(Index);
        }
    }
}
