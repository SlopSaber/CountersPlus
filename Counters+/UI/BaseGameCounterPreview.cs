using CountersPlus.ConfigModels;
using CountersPlus.Custom;
using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CountersPlus.UI
{
    internal static class BaseGameCounterPreview
    {
        internal static bool TryCombo(CounterPreviewContext preview)
        {
            CoreGameHUDController hud = FindHUD();
            ComboUIController source = hud?.GetComponentInChildren<ComboUIController>(true);
            if (source == null) return false;

            GameObject holder = CreateHolder(preview);
            GameObject clone = CloneGraphics(source.gameObject, holder.transform);
            Position(clone.transform, preview);
            TMP_Text count = clone.GetComponentsInChildren<TMP_Text>(true)
                .FirstOrDefault(text => text.name.IndexOf("combo", StringComparison.OrdinalIgnoreCase) >= 0 && int.TryParse(text.text, out _))
                ?? clone.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(text => int.TryParse(text.text, out _))
                ?? clone.GetComponentInChildren<TMP_Text>(true);
            if (count != null) count.text = "128";
            holder.SetActive(true);
            return true;
        }

        internal static bool TryMultiplier(CounterPreviewContext preview)
        {
            CoreGameHUDController hud = FindHUD();
            ScoreMultiplierUIController source = hud?.GetComponentInChildren<ScoreMultiplierUIController>(true);
            if (source == null) return false;

            GameObject holder = CreateHolder(preview);
            GameObject clone = CloneGraphics(source.gameObject, holder.transform);
            foreach (TMP_Text text in clone.GetComponentsInChildren<TMP_Text>(true))
                text.enabled = false;
            Position(clone.transform, preview);
            holder.SetActive(true);

            TMP_Text multiplier = preview.CreateText();
            multiplier.text = "x4";
            multiplier.fontSize = 4;
            multiplier.fontStyle = FontStyles.Italic;
            return true;
        }

        internal static bool TryScore(CounterPreviewContext preview, ScoreConfigModel settings, MainConfigModel mainConfig)
        {
            CoreGameHUDController hud = FindHUD();
            ScoreUIController scoreSource = hud?.GetComponentInChildren<ScoreUIController>(true);
            TMP_Text pointsSource = scoreSource?._scoreText;
            if (pointsSource == null || hud.relativeScoreGo == null || hud.immediateRankGo == null) return false;

            GameObject holder = CreateHolder(preview);
            GameObject pointsObject = CloneGraphics(pointsSource.gameObject, holder.transform);
            TMP_Text points = pointsObject.GetComponent<TMP_Text>();
            points.text = "567,890";
            float positionScale = preview.CanvasUtility.GetCanvasSettingsFromID(settings.CanvasID).PositionScale;
            Position(points.transform, preview, new Vector3(0, 1.91666f * 3f / positionScale, 0));

            if (settings.Mode != ScoreMode.RankOnly)
            {
                GameObject relativeObject = CloneGraphics(hud.relativeScoreGo, points.transform);
                relativeObject.transform.localPosition = pointsSource.transform.InverseTransformPoint(hud.relativeScoreGo.transform.position);
                TMP_Text relative = relativeObject.GetComponent<TMP_Text>();
                if (relative != null)
                {
                    relative.text = $"{94.26f.ToString($"F{settings.DecimalPrecision}")}%";
                    if (!mainConfig.ItalicText) relative.fontStyle = FontStyles.Normal;
                }
            }

            if (settings.Mode != ScoreMode.ScoreOnly)
            {
                GameObject rankObject = CloneGraphics(hud.immediateRankGo, points.transform);
                rankObject.transform.localPosition = pointsSource.transform.InverseTransformPoint(hud.immediateRankGo.transform.position);
                TMP_Text rank = rankObject.GetComponent<TMP_Text>();
                if (rank != null)
                {
                    rank.text = "S";
                    rank.color = settings.CustomRankColors ? settings.GetRankColorFromRank(RankModel.Rank.S) : Color.white;
                    if (!mainConfig.ItalicText) rank.fontStyle = FontStyles.Normal;
                }
            }

            if (!mainConfig.ItalicText) points.fontStyle = FontStyles.Normal;
            holder.SetActive(true);
            return true;
        }

        internal static bool TryProgress(CounterPreviewContext preview)
        {
            CoreGameHUDController hud = FindHUD();
            Canvas source = hud?.GetComponentInChildren<SongProgressUIController>(true)?.GetComponent<Canvas>();
            if (source == null) return false;

            GameObject holder = CreateHolder(preview);
            GameObject clone = CloneGraphics(source.gameObject, holder.transform);
            Position(clone.transform, preview, new Vector3(0, -0.25f, 0));
            holder.SetActive(true);
            return true;
        }

        internal static bool TryMultiplayerRank(CounterPreviewContext preview, MainConfigModel mainConfig)
        {
            MultiplayerPositionHUDController source = Resources.FindObjectsOfTypeAll<MultiplayerPositionHUDController>()
                .FirstOrDefault(controller => controller.gameObject.scene.name == "TutorialGameplay");
            if (source == null) return false;

            GameObject holder = CreateHolder(preview);
            GameObject clone = CloneGraphics(source.gameObject, holder.transform);
            Position(clone.transform, preview, new Vector3(0, -0.25f, 0));
            if (!mainConfig.ItalicText)
            {
                foreach (TMP_Text text in clone.GetComponentsInChildren<TMP_Text>(true))
                    text.fontStyle = FontStyles.Normal;
            }
            holder.SetActive(true);
            return true;
        }

        private static CoreGameHUDController FindHUD() => Resources.FindObjectsOfTypeAll<CoreGameHUDController>()
            .FirstOrDefault(hud => hud.gameObject.scene.name == "TutorialGameplay");

        private static GameObject CreateHolder(CounterPreviewContext preview)
        {
            Canvas canvas = preview.CanvasUtility.GetCanvasFromID(preview.Settings.CanvasID);
            GameObject holder = new GameObject("Base Game Counter Preview", typeof(RectTransform));
            holder.SetActive(false);
            holder.transform.SetParent(canvas.transform, false);
            preview.Track(holder);
            return holder;
        }

        private static GameObject CloneGraphics(GameObject source, Transform parent)
        {
            GameObject clone = UnityEngine.Object.Instantiate(source, parent, false);
            foreach (Behaviour behaviour in clone.GetComponentsInChildren<Behaviour>(true))
            {
                if (behaviour is TMP_Text || behaviour is Graphic || behaviour is Canvas)
                    continue;
                behaviour.enabled = false;
            }
            ActivateHierarchy(clone.transform);
            return clone;
        }

        private static void ActivateHierarchy(Transform transform)
        {
            transform.gameObject.SetActive(true);
            foreach (Transform child in transform)
                ActivateHierarchy(child);
        }

        private static void Position(Transform transform, CounterPreviewContext preview, Vector3? offset = null)
        {
            float positionScale = preview.CanvasUtility.GetCanvasSettingsFromID(preview.Settings.CanvasID).PositionScale;
            transform.localPosition = (preview.CanvasUtility.GetAnchoredPositionFromConfig(preview.Settings) + (offset ?? Vector3.zero)) * positionScale;
            transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0);
            transform.localEulerAngles = Vector3.zero;
        }
    }
}
