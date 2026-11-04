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
        // 0.75x of the strip's 18-unit cells, with a gap so icon artwork does not touch.
        private const float IconSize = 12f;
        private const float IconSpacing = 1.5f;
        private const float RowPitch = IconSize + IconSpacing;
        private const float Padding = 2f;
        private const float AnimationSeconds = 0.15f;
        private const float CloseDelaySeconds = 0.12f;
        private static readonly Color SelectedColor = new Color(0f, 0.75f, 1f);

        [Inject] private CounterCatalog catalog { get; set; }

        [UIComponent("list")] private CustomCellListTableData tableList { get; set; }
        [UIObject("flyout")] private GameObject flyout { get; set; }
        [UIComponent("flyout-grid")] private GridLayoutGroup flyoutGrid { get; set; }

        private readonly List<FlyoutCell> flyoutCells = new List<FlyoutCell>();
        private RectTransform flyoutRect;
        private RectTransform gridRect;
        private bool expanded;
        private bool suppressHoverUntilExit;
        private Coroutine animation;
        private PointerEventData flyoutPointer;
        private PointerEventData stripPointer;
        private float lastHoveredTime;
        private TMPro.TMP_Text hoverLabel;
        private ClickableImage hoveredIcon;
        private CanvasGroup stripGroup;
        private float openTop, openBottom, rowTop, rowBottom;

        public override string ResourceName => "CountersPlus.UI.BSML.SettingsSectionSelection.bsml";

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

            if (firstActivation) SetupFlyout();
            if (addedToHierarchy) catalog.ClearSelection();

            Collapse(false);
            ReloadCells();
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
            Collapse(false);
        }

        private void SetupFlyout()
        {
            flyoutRect = (RectTransform)flyout.transform;
            flyoutRect.anchorMin = flyoutRect.anchorMax = new Vector2(0.5f, 0.5f);
            flyoutRect.pivot = new Vector2(0.5f, 1f);
            flyoutRect.sizeDelta = new Vector2(Columns * IconSize + (Columns - 1) * IconSpacing + Padding * 2, IconSize + Padding * 2);
            flyout.AddComponent<RectMask2D>();
            // The panel stays as an invisible hover target so gaps between icons do not close the flyout.
            if (flyout.TryGetComponent(out Image panel)) panel.color = Color.clear;
            flyout.AddComponent<HoverRelay>().Entered = data => flyoutPointer = data;

            var center = new Vector2(0.5f, 0.5f);
            hoverLabel = BeatSaberUI.CreateCurvedUIText((RectTransform)flyout.transform.parent, string.Empty,
                center, center, Vector2.zero, new Vector2(30, 6));
            hoverLabel.alignment = TMPro.TextAlignmentOptions.Center;
            hoverLabel.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            hoverLabel.overflowMode = TMPro.TextOverflowModes.Overflow;
            hoverLabel.fontSize = 3f;
            hoverLabel.raycastTarget = false;
            hoverLabel.gameObject.SetActive(false);

            gridRect = (RectTransform)flyoutGrid.transform;
            gridRect.anchorMin = gridRect.anchorMax = new Vector2(0.5f, 1f);
            gridRect.pivot = new Vector2(0.5f, 1f);
            flyoutGrid.cellSize = new Vector2(IconSize, IconSize);
            flyoutGrid.spacing = new Vector2(IconSpacing, IconSpacing);
            flyoutGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            flyoutGrid.constraintCount = Columns;

            stripGroup = tableList.gameObject.AddComponent<CanvasGroup>();
            tableList.gameObject.AddComponent<HoverRelay>().Entered = data =>
            {
                stripPointer = data;
                if (!suppressHoverUntilExit && !expanded) Expand();
            };
        }

        // Pointer exit events are not reliable between nested graphics with the VR input module, so hover is
        // read back from the pointer that entered.
        private void Update()
        {
            if (suppressHoverUntilExit && !IsHovering(stripPointer, tableList.gameObject))
                suppressHoverUntilExit = false;

            if (!expanded) return;
            if (IsHovering(flyoutPointer, flyout) || IsHovering(stripPointer, tableList.gameObject))
            {
                lastHoveredTime = Time.unscaledTime;
                return;
            }
            if (Time.unscaledTime - lastHoveredTime > CloseDelaySeconds)
                Collapse(true);
        }

        private static bool IsHovering(PointerEventData pointer, GameObject target)
            => pointer != null && target != null && pointer.hovered.Contains(target);

        // Flat replacement for hover hints: the name is drawn over the hovered icon, like the strip's own cells.
        private void ShowHoverLabel(ClickableImage icon, string text)
        {
            if (!expanded) return;
            hoveredIcon = icon;
            var parent = (RectTransform)hoverLabel.transform.parent;
            Vector3 center = parent.InverseTransformPoint(icon.rectTransform.TransformPoint(icon.rectTransform.rect.center));
            hoverLabel.rectTransform.anchoredPosition = new Vector2(center.x, center.y);
            hoverLabel.text = text;
            hoverLabel.gameObject.SetActive(!string.IsNullOrEmpty(text));
            hoverLabel.transform.SetAsLastSibling();
        }

        private void HideHoverLabel(ClickableImage icon)
        {
            if (hoveredIcon != icon) return;
            hoveredIcon = null;
            hoverLabel.gameObject.SetActive(false);
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
                BSMLParser.Instance.Parse("<clickable-image id='icon' preserve-aspect='true' on-click='clicked'/>",
                    flyoutGrid.gameObject, cell);
                cell.Apply((CountersPlusListTableCell)items[i]);
                string name = items[i].Text;
                ClickableImage icon = cell.Icon;
                icon.PointerEnterEvent += _ => ShowHoverLabel(icon, name);
                icon.PointerExitEvent += _ => HideHoverLabel(icon);
                flyoutCells.Add(cell);
            }
            RefreshFlyoutSelection();
        }

        private void RefreshFlyoutSelection()
        {
            foreach (FlyoutCell cell in flyoutCells)
                cell.SetSelected(cell.Index == catalog.SelectedIndex);
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

            rowTop = stripCenter.y + IconSize / 2 + Padding;
            rowBottom = stripCenter.y - IconSize / 2 - Padding;
            openTop = rowTop + anchorRow * RowPitch;
            openBottom = rowBottom - (rows - 1 - anchorRow) * RowPitch;
            flyoutRect.anchoredPosition = new Vector2(stripCenter.x, rowTop);
            hoverLabel.gameObject.SetActive(false);
            lastHoveredTime = Time.unscaledTime;

            flyout.SetActive(true);
            flyout.transform.SetAsLastSibling();
            hoverLabel.transform.SetAsLastSibling();
            Animate(1f);
        }

        private void Collapse(bool animated)
        {
            expanded = false;
            if (flyoutRect == null) return;
            hoveredIcon = null;
            hoverLabel.gameObject.SetActive(false);
            if (animated && isActiveAndEnabled && flyout.activeSelf)
            {
                Animate(0f);
                return;
            }
            StopAnimation();
            openAmount = 0f;
            stripGroup.alpha = 1f;
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
            // The strip row is covered by the flyout's anchored row, so it fades out rather than showing through.
            stripGroup.alpha = 1f - amount;
            float top = Mathf.Lerp(rowTop, openTop, amount);
            float bottom = Mathf.Lerp(rowBottom, openBottom, amount);
            flyoutRect.anchoredPosition = new Vector2(flyoutRect.anchoredPosition.x, top);
            flyoutRect.sizeDelta = new Vector2(flyoutRect.sizeDelta.x, top - bottom);
            // Keep the grid fixed in place while the mask grows around it.
            gridRect.anchoredPosition = new Vector2(0, (openTop - Padding) - top);
        }

        private sealed class HoverRelay : MonoBehaviour, IPointerEnterHandler
        {
            public Action<PointerEventData> Entered;

            public void OnPointerEnter(PointerEventData eventData) => Entered?.Invoke(eventData);
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
