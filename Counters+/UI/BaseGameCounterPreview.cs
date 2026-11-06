using CountersPlus.ConfigModels;
using CountersPlus.Custom;
using HMUI;
using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CountersPlus.UI
{
    internal static class BaseGameCounterPreview
    {
        private const float HudUnitScale = 0.01f;

        internal static void Combo(CounterPreviewContext preview, MainConfigModel mainConfig)
        {
            GameObject holder = CreateHudHolder(preview);
            Sprite line = FindSprite("Rect16");
            AddImage(holder.transform, "Line0", line, Color.white, new Vector2(100, 6), new Vector2(0, 35), Image.Type.Sliced);
            AddImage(holder.transform, "Line1", line, Color.white, new Vector2(100, 6), new Vector2(0, -35), Image.Type.Sliced);

            TMP_Text label = AddText(preview, holder.transform, "Combo", 30, new Vector2(120, 30), new Vector2(0, 13));
            label.fontStyle = FontStyles.UpperCase | (mainConfig.ItalicText ? FontStyles.Italic : FontStyles.Normal);
            label.transform.localScale = new Vector3(0.7f, 1, 1);

            TMP_Text count = AddText(preview, holder.transform, "128", 40, new Vector2(100, 40), new Vector2(0, -16));
            count.fontStyle = mainConfig.ItalicText ? FontStyles.Italic : FontStyles.Normal;
            holder.SetActive(true);
        }

        internal static void Multiplier(CounterPreviewContext preview)
        {
            GameObject holder = CreateHudHolder(preview);
            Sprite circle = FindSprite("Circle");
            AddImage(holder.transform, "BGCircle", circle, new Color(1, 1, 1, 0.25f), new Vector2(100, 100), Vector2.zero, Image.Type.Simple);
            Image progress = AddImage(holder.transform, "FGCircle", circle, Color.white, new Vector2(100, 100), Vector2.zero, Image.Type.Filled);
            progress.fillMethod = Image.FillMethod.Radial360;
            progress.fillOrigin = 2;
            progress.fillClockwise = true;
            progress.fillAmount = 0.5f;
            holder.SetActive(true);

            TMP_Text multiplier = preview.CreateText();
            multiplier.text = "x4";
            multiplier.fontSize = 4;
            multiplier.fontStyle = FontStyles.Italic;
        }

        private static GameObject CreateHudHolder(CounterPreviewContext preview)
        {
            GameObject holder = CreateHolder(preview);
            Position(holder.transform, preview);
            holder.transform.localScale = Divide(Vector3.one * HudUnitScale, holder.transform.parent.lossyScale);
            return holder;
        }

        private static Sprite FindSprite(string name) => Resources.FindObjectsOfTypeAll<Sprite>().FirstOrDefault(sprite => sprite.name == name);

        private static Image AddImage(Transform parent, string name, Sprite sprite, Color color, Vector2 size, Vector2 position, Image.Type type)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            ImageView image = gameObject.AddComponent<ImageView>();
            image.material = BeatSaberMarkupLanguage.Utilities.ImageResources.NoGlowMat;
            image.sprite = sprite;
            image.type = sprite == null && type == Image.Type.Sliced ? Image.Type.Simple : type;
            image.color = color;
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = size;
            image.rectTransform.anchoredPosition = position;
            return image;
        }

        private static TMP_Text AddText(CounterPreviewContext preview, Transform parent, string value, float fontSize, Vector2 size, Vector2 position)
        {
            TMP_Text text = preview.CreateText();
            text.transform.SetParent(parent, false);
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            text.rectTransform.sizeDelta = size;
            text.rectTransform.anchoredPosition = position;
            text.transform.localRotation = Quaternion.identity;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            return text;
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
            MultiplayerPositionHUDController[] candidates = Resources.FindObjectsOfTypeAll<MultiplayerPositionHUDController>();
            MultiplayerPositionHUDController source = candidates.FirstOrDefault(controller => controller.gameObject.scene.name == "TutorialGameplay")
                ?? candidates.FirstOrDefault(controller => controller.gameObject.scene.IsValid() && controller.gameObject.scene.isLoaded);
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

        private static CoreGameHUDController FindHUD()
        {
            CoreGameHUDController[] candidates = Resources.FindObjectsOfTypeAll<CoreGameHUDController>();
            return candidates.FirstOrDefault(hud => hud.gameObject.scene.name == "TutorialGameplay")
                ?? candidates.FirstOrDefault(hud => hud.gameObject.scene.IsValid() && hud.gameObject.scene.isLoaded);
        }

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
            clone.transform.localScale = Divide(source.transform.lossyScale, parent.lossyScale);
            foreach (Behaviour behaviour in clone.GetComponentsInChildren<Behaviour>(true))
            {
                if (behaviour is TMP_Text || behaviour is Graphic || behaviour is Canvas ||
                    behaviour is CanvasGroup || behaviour is CurvedCanvasSettings)
                    continue;
                behaviour.enabled = false;
            }
            ActivateHierarchy(clone.transform);
            return clone;
        }

        private static Vector3 Divide(Vector3 value, Vector3 by) => new Vector3(
            by.x == 0 ? value.x : value.x / by.x,
            by.y == 0 ? value.y : value.y / by.y,
            by.z == 0 ? value.z : value.z / by.z);

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
