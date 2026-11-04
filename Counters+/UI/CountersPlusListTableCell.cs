using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Parser;
using HMUI;
using System.Linq;
using TMPro;
using UnityEngine;

namespace CountersPlus.UI
{
    public class CountersPlusListTableCell : CustomListTableData.CustomCellInfo
    {
        public static readonly Color AccentColor = new Color(1f, 0.72f, 0.25f);

        public int CellIdx { get; private set; } = 0;

        public bool IsAccent { get; }

        [UIParams] private BSMLParserParams parserParams = null;

        public CountersPlusListTableCell(int idx, string text, string subtext, Sprite icon = null, bool isAccent = false) : base(text, subtext, icon)
        {
            CellIdx = idx;
            IsAccent = isAccent;
        }

        [UIAction("#post-parse")]
        private void Parsed()
        {
            foreach (var image in parserParams.GetObjectsWithTag("coverImage").Select(x => x.GetComponent<ImageView>()))
            {
                image.sprite = Icon;
                if (IsAccent) image.color *= AccentColor;
            }

            foreach (var infoText in parserParams.GetObjectsWithTag("infoText").Select(x => x.GetComponent<TMP_Text>()))
                infoText.text = Text;

            foreach (var accentText in parserParams.GetObjectsWithTag("accentText").Select(x => x.GetComponent<TMP_Text>()))
            {
                accentText.gameObject.SetActive(IsAccent);
                accentText.text = Text;
                accentText.color = AccentColor;
            }
        }
    }
}
