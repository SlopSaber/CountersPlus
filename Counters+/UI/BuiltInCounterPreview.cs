using CountersPlus.ConfigModels;
using CountersPlus.Counters;
using CountersPlus.Custom;
using HMUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CountersPlus.UI
{
    internal static class BuiltInCounterPreview
    {
        internal static void Render(CounterPreviewContext preview, MainConfigModel mainConfig)
        {
            switch (preview.Settings)
            {
                case MissedConfigModel _:
                    Basic(preview, "Misses", "2");
                    break;
                case NoteConfigModel notes:
                    Basic(preview, "Notes", notes.ShowPercentage
                        ? $"123 / 125 - {(123f / 125f * 100).ToString($"F{notes.DecimalPrecision}")}%"
                        : "123 / 125");
                    break;
                case ProgressConfigModel progress:
                    if (progress.Mode != ProgressMode.BaseGame || !BaseGameCounterPreview.TryProgress(preview))
                        Progress(preview, progress);
                    break;
                case ScoreConfigModel score:
                    if (!BaseGameCounterPreview.TryScore(preview, score, mainConfig))
                        Score(preview, score);
                    break;
                case ComboConfigModel _:
                    if (!BaseGameCounterPreview.TryCombo(preview))
                        Text(preview, "128", 5);
                    break;
                case MultiplierConfigModel _:
                    if (!BaseGameCounterPreview.TryMultiplier(preview))
                    {
                        TMP_Text multiplier = Text(preview, "x4", 4);
                        multiplier.fontStyle = FontStyles.Italic;
                    }
                    break;
                case PBConfigModel pb:
                    PersonalBest(preview, pb, mainConfig);
                    break;
                case SpeedConfigModel speed:
                    Speed(preview, speed);
                    break;
                case CutConfigModel cut:
                    Cut(preview, cut);
                    break;
                case SpinometerConfigModel spin:
                    Basic(preview, "Spinometer", spin.Mode == SpinometerMode.SplitAverage
                        ? "<color=#FFB966>720</color> | <color=#FFC57E>840</color>"
                        : "<color=#FFB966>780</color>");
                    break;
                case NotesLeftConfigModel left:
                    if (left.LabelAboveCount)
                        Basic(preview, "Notes Remaining", "456");
                    else
                        Text(preview, "Notes Remaining: 456", 2);
                    break;
                case FailConfigModel fail:
                    Basic(preview, fail.ShowRestartsInstead ? "Restarts" : "Fails", "1");
                    break;
                case MultiplayerRankConfigModel _:
                    if (!BaseGameCounterPreview.TryMultiplayerRank(preview, mainConfig))
                        Text(preview, "#2 / 8", 4);
                    break;
                default:
                    preview.CreateText().text = preview.Settings.DisplayName;
                    break;
            }
        }

        private static TMP_Text Text(CounterPreviewContext preview, string value, float fontSize, Vector3? offset = null)
        {
            TMP_Text text = preview.CreateText(offset);
            text.text = value;
            text.fontSize = fontSize;
            return text;
        }

        private static void Basic(CounterPreviewContext preview, string label, string value)
        {
            Text(preview, label, 3);
            float positionScale = preview.CanvasUtility.GetCanvasSettingsFromID(preview.Settings.CanvasID).PositionScale;
            Text(preview, value, 4, new Vector3(0, -0.4f * (10 / positionScale), 0));
        }

        private static void Progress(CounterPreviewContext preview, ProgressConfigModel settings)
        {
            string value = settings.Mode switch
            {
                ProgressMode.Percent => settings.ProgressTimeLeft ? "57%" : "43%",
                ProgressMode.TimeInBeats => settings.ProgressTimeLeft ? "128.50" : "96.50",
                _ => settings.ProgressTimeLeft ? "2:22" : "1:23"
            };
            TMP_Text text = Text(preview, value, 4);
            if (settings.Mode == ProgressMode.Percent)
                return;

            Canvas canvas = preview.CanvasUtility.GetCanvasFromID(settings.CanvasID);
            ImageView background = ProgressCounter.CreateRing(canvas);
            preview.Track(background.gameObject);
            background.rectTransform.anchoredPosition = text.rectTransform.anchoredPosition;
            background.CrossFadeAlpha(0.05f, 1f, false);
            background.transform.localScale = Vector3.one * 1.175f / 10;
            background.type = Image.Type.Simple;

            ImageView ring = ProgressCounter.CreateRing(canvas);
            preview.Track(ring.gameObject);
            ring.rectTransform.anchoredPosition = text.rectTransform.anchoredPosition;
            ring.transform.localScale = Vector3.one * 1.175f / 10;
            ring.fillAmount = settings.IncludeRing ? (settings.ProgressTimeLeft ? 0.57f : 0.43f) : 0.43f;
        }

        private static void Score(CounterPreviewContext preview, ScoreConfigModel settings)
        {
            float positionScale = preview.CanvasUtility.GetCanvasSettingsFromID(settings.CanvasID).PositionScale;
            Text(preview, "567,890", 4, new Vector3(0, 1.91666f * 3f / positionScale, 0));
            if (settings.Mode != ScoreMode.RankOnly)
                Text(preview, $"{94.26f.ToString($"F{settings.DecimalPrecision}")}%", 3);
            if (settings.Mode != ScoreMode.ScoreOnly)
            {
                TMP_Text rank = Text(preview, "S", 3, new Vector3(0, 0.4f, 0));
                rank.color = settings.CustomRankColors ? settings.GetRankColorFromRank(RankModel.Rank.S) : Color.white;
            }
        }

        private static void PersonalBest(CounterPreviewContext preview, PBConfigModel settings, MainConfigModel mainConfig)
        {
            TMP_Text text;
            if (mainConfig.ScoreConfig.Enabled && settings.UnderScore)
            {
                ScoreConfigModel score = mainConfig.ScoreConfig;
                float positionScale = preview.CanvasUtility.GetCanvasSettingsFromID(score.CanvasID).PositionScale;
                text = preview.CreateText(score, new Vector3(0, -1.85f * 3f / positionScale, 0));
            }
            else
                text = preview.CreateText();

            text.alignment = TextAlignmentOptions.Top;
            text.fontSize = settings.TextSize;
            text.text = $"PB: {95.12f.ToString($"F{settings.DecimalPrecision}")}%";
            text.color = settings.DefaultColor;
        }

        private static void Speed(CounterPreviewContext preview, SpeedConfigModel settings)
        {
            string average = settings.Mode == SpeedMode.SplitAverage || settings.Mode == SpeedMode.SplitBoth
                ? $"{7.42f.ToString($"F{settings.DecimalPrecision}")} | {8.14f.ToString($"F{settings.DecimalPrecision}")}"
                : 7.78f.ToString($"F{settings.DecimalPrecision}");
            string top = 11.98f.ToString($"F{settings.DecimalPrecision}");

            if (settings.Mode == SpeedMode.Top5Sec)
                Basic(preview, "Recent Top Speed", top);
            else
            {
                Basic(preview, "Average Speed", average);
                if (settings.Mode == SpeedMode.Both || settings.Mode == SpeedMode.SplitBoth)
                {
                    Text(preview, "Recent Top Speed", 3, new Vector3(0, -1, 0));
                    Text(preview, top, 4, new Vector3(0, -1.4f, 0));
                }
            }
        }

        private static void Cut(CounterPreviewContext preview, CutConfigModel settings)
        {
            Text(preview, "Average Cut", 3);
            string value = settings.SeparateCutValues
                ? $"{68.4f.ToString($"F{settings.AveragePrecision}")}\n{29.1f.ToString($"F{settings.AveragePrecision}")}\n{14.7f.ToString($"F{settings.AveragePrecision}")}"
                : 112.2f.ToString($"F{settings.AveragePrecision}");

            if (settings.SeparateSaberCounts)
            {
                TMP_Text right = Text(preview, value, 4, new Vector3(0.2f, -0.2f, 0));
                right.lineSpacing = -26;
                right.alignment = TextAlignmentOptions.TopLeft;
            }

            TMP_Text left = Text(preview, value, 4,
                settings.SeparateSaberCounts ? new Vector3(-0.2f, -0.2f, 0) : new Vector3(0, -0.2f, 0));
            left.lineSpacing = -26;
            left.alignment = settings.SeparateSaberCounts ? TextAlignmentOptions.TopRight : TextAlignmentOptions.Top;
        }
    }
}
