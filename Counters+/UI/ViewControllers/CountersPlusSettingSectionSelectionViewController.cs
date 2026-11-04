using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

namespace CountersPlus.UI.ViewControllers
{
    public class CountersPlusSettingSectionSelectionViewController : BSMLResourceViewController
    {
        private const int Columns = 7;
        private const float CellSize = 18f;
        private const float Padding = 2f;
        private const float AnimationSeconds = 0.15f;
        private static readonly Color SelectedColor = new Color(0f, 0.75f, 1f);

        [Inject] private CounterCatalog catalog { get; set; }

        [UIComponent("list")] private CustomCellListTableData tableList { get; set; }
        [UIComponent("left-button")] private Button leftButton { get; set; }
        [UIComponent("right-button")] private Button rightButton { get; set; }
        [UIObject("flyout")] private GameObject flyout { get; set; }
        [UIComponent("flyout-grid")] private GridLayoutGroup flyoutGrid { get; set; }

        private readonly List<FlyoutCell> flyoutCells = new List<FlyoutCell>();
        private ScrollView scrollView;
        private RectTransform flyoutRect;
        private RectTransform gridRect;
        private bool expanded;
        private bool suppressHoverUntilExit;
        private Coroutine animation;
        private float openTop, openBottom, rowTop, rowBottom;

        public override string ResourceName => "CountersPlus.UI.BSML.SettingsSectionSelection.bsml";

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

            scrollView = tableList.TableView.scrollView;
            scrollView.scrollPositionChangedEvent += ScrollView_scrollPositionChangedEvent;

            if (firstActivation) SetupFlyout();
            if (addedToHierarchy) catalog.ClearSelection();

            Collapse(false);
            ReloadCells();
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
            scrollView.scrollPositionChangedEvent -= ScrollView_scrollPositionChangedEvent;
            Collapse(false);
        }

        [UIAction("#post-parse")]
        private void Parsed()
        {
            var colors = rightButton.colors;
            colors.disabledColor = Color.black;
            rightButton.colors = colors;
            leftButton.colors = colors;
        }

        private void SetupFlyout()
        {
            flyoutRect = (RectTransform)flyout.transform;
            flyoutRect.anchorMin = flyoutRect.anchorMax = new Vector2(0.5f, 0.5f);
            flyoutRect.pivot = new Vector2(0.5f, 1f);
            flyoutRect.sizeDelta = new Vector2(Columns * CellSize + Padding * 2, CellSize + Padding * 2);
            flyout.AddComponent<RectMask2D>();
            flyout.AddComponent<HoverRelay>().Exited = () =>
            {
                if (expanded) Collapse(true);
            };

            gridRect = (RectTransform)flyoutGrid.transform;
            gridRect.anchorMin = gridRect.anchorMax = new Vector2(0.5f, 1f);
            gridRect.pivot = new Vector2(0.5f, 1f);
            flyoutGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            flyoutGrid.constraintCount = Columns;

            var strip = tableList.gameObject.AddComponent<HoverRelay>();
            strip.Entered = () =>
            {
                if (!suppressHoverUntilExit && !expanded) Expand();
            };
            strip.Exited = () => suppressHoverUntilExit = false;
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

        private void SelectFromFlyout(int idx)
        {
            catalog.Select(idx);
            RefreshFlyoutSelection();
            suppressHoverUntilExit = true;
            Collapse(true);
            tableList.TableView.SelectCellWithIdx(idx, false);
            tableList.TableView.ScrollToCellWithIdx(idx, TableView.ScrollPositionType.Center, false);
        }

        // The row holding the current selection stays over the strip; earlier rows open upward and later rows downward.
        private void Expand()
        {
            if (flyoutRect == null || flyoutCells.Count == 0) return;
            expanded = true;

            int rows = Mathf.CeilToInt(flyoutCells.Count / (float)Columns);
            int anchorRow = catalog.SelectedIndex >= 0 ? catalog.SelectedIndex / Columns : 0;

            tableList.TableView.ScrollToCellWithIdx(anchorRow * Columns, TableView.ScrollPositionType.Beginning, false);

            var listRect = (RectTransform)tableList.transform;
            var parent = (RectTransform)flyoutRect.parent;
            Vector3 stripCenter = parent.InverseTransformPoint(listRect.TransformPoint(listRect.rect.center));

            rowTop = stripCenter.y + CellSize / 2 + Padding;
            rowBottom = stripCenter.y - CellSize / 2 - Padding;
            openTop = rowTop + anchorRow * CellSize;
            openBottom = rowBottom - (rows - 1 - anchorRow) * CellSize;
            flyoutRect.anchoredPosition = new Vector2(stripCenter.x, rowTop);

            flyout.SetActive(true);
            flyout.transform.SetAsLastSibling();
            Animate(1f);
        }

        private void Collapse(bool animated)
        {
            expanded = false;
            if (flyoutRect == null) return;
            if (animated && isActiveAndEnabled && flyout.activeSelf)
            {
                Animate(0f);
                return;
            }
            StopAnimation();
            openAmount = 0f;
            flyout.SetActive(false);
        }

        private void StopAnimation()
        {
            if (animation == null) return;
            StopCoroutine(animation);
            animation = null;
        }

        private float openAmount;

        private void Animate(float target)
        {
            StopAnimation();
            if (!isActiveAndEnabled)
            {
                ApplyOpenAmount(target);
                flyout.SetActive(target > 0f);
                return;
            }
            animation = StartCoroutine(AnimateTo(target));
        }

        private IEnumerator AnimateTo(float target)
        {
            float start = openAmount;
            for (float t = 0; t < AnimationSeconds; t += Time.deltaTime)
            {
                float eased = 1f - Mathf.Pow(1f - t / AnimationSeconds, 3f);
                ApplyOpenAmount(Mathf.Lerp(start, target, eased));
                yield return null;
            }
            ApplyOpenAmount(target);
            if (target <= 0f) flyout.SetActive(false);
            animation = null;
        }

        private void ApplyOpenAmount(float amount)
        {
            openAmount = amount;
            float top = Mathf.Lerp(rowTop, openTop, amount);
            float bottom = Mathf.Lerp(rowBottom, openBottom, amount);
            flyoutRect.anchoredPosition = new Vector2(flyoutRect.anchoredPosition.x, top);
            flyoutRect.sizeDelta = new Vector2(flyoutRect.sizeDelta.x, top - bottom);
            // Keep the grid fixed in place while the mask grows around it.
            gridRect.anchoredPosition = new Vector2(0, (openTop - Padding) - top);
        }

        private sealed class HoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public Action Entered;
            public Action Exited;

            public void OnPointerEnter(PointerEventData eventData) => Entered?.Invoke();

            public void OnPointerExit(PointerEventData eventData)
            {
                if (eventData.fullyExited) Exited?.Invoke();
            }
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
