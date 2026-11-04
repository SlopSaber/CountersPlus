using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using HMUI;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace CountersPlus.UI.ViewControllers
{
    /// <summary>
    /// Expanded grid of every counter selection entry, shown on the main screen like the base game's playlist grid.
    /// </summary>
    public class CountersPlusCounterGridViewController : ViewController
    {
        private const int Columns = 5;
        private static readonly Color SelectedColor = new Color(0f, 0.75f, 1f);

        private const string Layout =
            "<bg>" +
            "<vertical bg='round-rect-panel' pad='3' spacing='2' anchor-min-x='0' anchor-max-x='1' anchor-min-y='0' anchor-max-y='1' size-delta-x='-4' size-delta-y='-4' vertical-fit='Unconstrained' horizontal-fit='Unconstrained' child-control-height='true' child-expand-height='false'>" +
            "<text text='Counters' align='Center' font-size='6'/>" +
            "<vertical pref-height='62' child-control-height='true' child-expand-height='true'>" +
            "<scroll-view><grid id='grid' cell-size-x='20' cell-size-y='22' spacing-x='2' spacing-y='2' child-align='UpperCenter'/></scroll-view>" +
            "</vertical>" +
            "</vertical>" +
            "</bg>";

        private const string CellLayout =
            "<vertical spacing='0.5' child-control-height='false' child-expand-height='false' child-align='UpperCenter'>" +
            "<clickable-image id='icon' pref-width='16' pref-height='16' size-delta-x='16' size-delta-y='16' preserve-aspect='true' on-click='clicked'/>" +
            "<text id='label' font-size='2.5' align='Center' size-delta-x='20' size-delta-y='4'/>" +
            "</vertical>";

        [Inject] private CounterCatalog catalog { get; set; }

        [UIComponent("grid")] private GridLayoutGroup grid = null;

        private readonly List<GameObject> cells = new List<GameObject>();

        internal Action<int> SelectionHandler { get; set; }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            if (firstActivation)
            {
                BSMLParser.Instance.Parse(Layout, gameObject, this);
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = Columns;
            }
            Rebuild();
        }

        private void Rebuild()
        {
            foreach (GameObject cell in cells)
                Destroy(cell);
            cells.Clear();

            for (int i = 0; i < catalog.Count; i++)
            {
                CountersPlusListTableCell info = catalog.CellInfo(i);
                GridCell host = new GridCell(i, this);
                BSMLParser.Instance.Parse(CellLayout, grid.gameObject, host);
                host.Apply(info, i == catalog.SelectedIndex);
                cells.Add(grid.transform.GetChild(grid.transform.childCount - 1).gameObject);
            }
        }

        private void Clicked(int idx) => SelectionHandler?.Invoke(idx);

        private class GridCell
        {
            private readonly int index;
            private readonly CountersPlusCounterGridViewController owner;

            [UIComponent("icon")] private ClickableImage icon = null;
            [UIComponent("label")] private TextMeshProUGUI label = null;

            public GridCell(int index, CountersPlusCounterGridViewController owner)
            {
                this.index = index;
                this.owner = owner;
            }

            public void Apply(CountersPlusListTableCell info, bool selected)
            {
                icon.sprite = info.Icon;
                Color color = selected ? SelectedColor : info.IsAccent ? CountersPlusListTableCell.AccentColor : Color.white;
                icon.DefaultColor = color;
                icon.HighlightColor = Color.Lerp(color, Color.white, 0.5f) * 0.8f;
                label.text = info.Text;
                label.color = info.IsAccent ? CountersPlusListTableCell.AccentColor : Color.white;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Ellipsis;
            }

            [UIAction("clicked")]
            private void Clicked() => owner.Clicked(index);
        }
    }
}
