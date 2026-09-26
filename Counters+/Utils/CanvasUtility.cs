using BeatSaberMarkupLanguage;
using CountersPlus.ConfigModels;
using HMUI;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Zenject;

namespace CountersPlus.Utils
{
    public class CanvasUtility
    {
        private readonly int UILayer = LayerMask.NameToLayer("UI");

        private Dictionary<int, Canvas> CanvasIDToCanvas = new Dictionary<int, Canvas>();
        private Dictionary<Canvas, HUDCanvas> CanvasToSettings = new Dictionary<Canvas, HUDCanvas>();
        internal Dictionary<int, Canvas>.ValueCollection Canvases => CanvasIDToCanvas.Values;
        private Canvas energyCanvas = null;
        private MainConfigModel mainConfig;

        private float hudDepth = 7f;

        // Using the magical power of Zenject™, we magically find ourselves with an instance of
        // our HUDConfigModel and the CoreGameHUDController.
        internal CanvasUtility(HUDConfigModel hudConfig,
            MainConfigModel mainConfig,
            [InjectOptional] GameplayCoreSceneSetupData data,
            [InjectOptional] CoreGameHUDController coreGameHUD,
            [InjectOptional] MultiplayerPositionHUDController multiplayerPositionHUD)
        {
            this.mainConfig = mainConfig;
            if (coreGameHUD != null)
            {
                hudDepth = coreGameHUD.GetComponentInChildren<ComboUIController>().transform.position.z;

                energyCanvas = coreGameHUD.energyPanelGo.GetComponent<Canvas>();

                // Hide base game elements if needed
                if (mainConfig.HideCombo) HideBaseGameHUDElement<ComboUIController>(coreGameHUD);
                if (mainConfig.HideMultiplier) HideBaseGameHUDElement<ScoreMultiplierUIController>(coreGameHUD);

                if (mainConfig.HideMultiplayerRank && multiplayerPositionHUD != null)
                {
                    multiplayerPositionHUD.gameObject.SetActive(false);
                }
            }

            RefreshAllCanvases(hudConfig, data, coreGameHUD);
        }

        public void RefreshAllCanvases(HUDConfigModel hudConfig, GameplayCoreSceneSetupData data = null, CoreGameHUDController coreGameHUD = null)
        {
            CanvasIDToCanvas.Clear();
            CanvasToSettings.Clear();
            CanvasIDToCanvas.Add(-1, CreateCanvasWithConfig(hudConfig.MainCanvasSettings));
            CanvasToSettings.Add(CanvasIDToCanvas[-1], hudConfig.MainCanvasSettings);
            if (coreGameHUD != null && hudConfig.MainCanvasSettings.ParentedToBaseGameHUD)
            {
                SoftParent softParent = CanvasIDToCanvas[-1].gameObject.AddComponent<SoftParent>();
                softParent.AssignParent(coreGameHUD.transform);
            }
            for (int i = 0; i < hudConfig.OtherCanvasSettings.Count; i++)
            {
                HUDCanvas canvasSettings = hudConfig.OtherCanvasSettings[i];
                RegisterNewCanvas(canvasSettings, i);

                if (coreGameHUD != null && hudConfig.OtherCanvasSettings[i].ParentedToBaseGameHUD)
                {
                    Transform parent = coreGameHUD.transform;
                    //if (HUDType == GameplayCoreHUDInstaller.HudType.Flying) parent = coreGameHUD.transform.GetChild(0);
                    SoftParent softParent = CanvasIDToCanvas[i].gameObject.AddComponent<SoftParent>();
                    softParent.AssignParent(parent);
                }
            }
        }

        public void RegisterNewCanvas(HUDCanvas canvasSettings, int id)
        {
            Canvas canvas = CreateCanvasWithConfig(canvasSettings);
            CanvasIDToCanvas.Add(id, canvas);
            CanvasToSettings.Add(canvas, canvasSettings);
        }

        public void UnregisterCanvas(int id)
        {
            Canvas canvas = CanvasIDToCanvas[id];

            CanvasIDToCanvas.Remove(id);
            CanvasToSettings.Remove(canvas);
            Object.Destroy(canvas.gameObject);
        }

        public Canvas CreateCanvasWithConfig(HUDCanvas canvasSettings)
        {
            GameObject canvasGameObject = new GameObject($"Counters+ | {canvasSettings.Name} Canvas");
            canvasGameObject.layer = UILayer;

            Vector3 canvasPos = canvasSettings.Position;

            if (canvasSettings.MatchBaseGameHUDDepth)
            {
                canvasPos.Set(canvasPos.x, canvasPos.y, hudDepth);
            }

            Vector3 canvasRot = canvasSettings.Rotation;
            float canvasSize = canvasSettings.Size;

            Canvas canvas = canvasGameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            canvasGameObject.transform.localScale = Vector3.one / canvasSize;
            canvasGameObject.transform.position = canvasPos;
            canvasGameObject.transform.rotation = Quaternion.Euler(canvasRot);

            CurvedCanvasSettings curvedCanvasSettings = canvasGameObject.AddComponent<CurvedCanvasSettings>();
            curvedCanvasSettings.SetRadius(canvasSettings.CurveRadius);

            // Inherit canvas properties from the Energy Bar to ignore the shockwave effect.
            // However, a caveat as that, when viewing through walls, UI elements will not appear.
            if (canvasSettings.IgnoreShockwaveEffect && energyCanvas != null)
            {
                canvas.overrideSorting = energyCanvas.overrideSorting;
                canvas.sortingLayerID = energyCanvas.sortingLayerID;
                canvas.sortingLayerName = energyCanvas.sortingLayerName;
                canvas.sortingOrder = energyCanvas.sortingOrder;
                canvas.gameObject.layer = energyCanvas.gameObject.layer;
            }

            return canvas;
        }

#nullable enable
        public Canvas? GetCanvasFromID(int id)
            => id == -1 || id >= mainConfig.HUDConfig.OtherCanvasSettings.Count
                ? CanvasIDToCanvas[-1]
                : CanvasIDToCanvas[id];

        public HUDCanvas? GetCanvasSettingsFromID(int id)
            => id == -1 || id >= mainConfig.HUDConfig.OtherCanvasSettings.Count
                ? mainConfig.HUDConfig.MainCanvasSettings
                : mainConfig.HUDConfig.OtherCanvasSettings[id];

        public HUDCanvas? GetCanvasSettingsFromCanvas(Canvas canvas)
        {
            if (CanvasToSettings.TryGetValue(canvas, out HUDCanvas settings)) return settings;
            return null;

        }
#nullable restore

        public TMP_Text CreateTextFromSettings(ConfigModel settings, Vector3? offset = null)
        {
            Canvas canvasToApply = GetCanvasFromID(settings.CanvasID);
            if (canvasToApply == null)
            {
                var hudSettings = GetCanvasSettingsFromID(settings.CanvasID);
                canvasToApply = CreateCanvasWithConfig(hudSettings);
                CanvasIDToCanvas[settings.CanvasID] = canvasToApply;
                CanvasToSettings.Add(canvasToApply, hudSettings);
            }
            return CreateText(canvasToApply, GetAnchoredPositionFromConfig(settings), offset);
        }

        public TMP_Text CreateText(Canvas canvas, Vector3 anchoredPosition, Vector3? offset = null)
        {
            var rectTransform = canvas.transform as RectTransform;
            rectTransform.sizeDelta = new Vector2(100, 50);

            float posScaleFactor = 10;
            if (CanvasToSettings.TryGetValue(canvas, out HUDCanvas settings))
            {
                posScaleFactor = settings.PositionScale;
            }

            if (offset != null)
            {
                anchoredPosition += offset.Value;
            }

            TMP_Text tmp_text = BeatSaberUI.CreateCurvedUIText(rectTransform, "", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition * posScaleFactor, new Vector2(60, 10));
            tmp_text.gameObject.layer = UILayer;
            tmp_text.alignment = TextAlignmentOptions.Center;
            tmp_text.fontSize = 4f;
            tmp_text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 2f);
            tmp_text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 2f);
            tmp_text.textWrappingMode = TextWrappingModes.NoWrap;
            tmp_text.overflowMode = TextOverflowModes.Overflow;

            if (mainConfig.ItalicText)
            {
                tmp_text.fontStyle = FontStyles.Italic;
                if (offset != null)
                {
                    tmp_text.rectTransform.anchoredPosition += new Vector2(Mathf.Abs(offset.Value.x) * -1, 0)
                        * posScaleFactor
                        * 0.18f
                        * tmp_text.fontSize;
                }
            }

            return tmp_text;
        }

        // TODO: holy shit can i please rewrite this method and make it not gross
        public Vector3 GetAnchoredPositionFromConfig(ConfigModel settings)
        {
            float comboOffset = mainConfig.ComboOffset;
            float multOffset = mainConfig.MultiplierOffset;
            CounterPositions position = settings.Position;
            float index = settings.Distance;
            var pos = new Vector3(); // Base position
            float belowEnergyOffset = -1.5f;
            float aboveHighwayOffset = 0.75f;

            float X = 3.2f;

            var canvasSettings = GetCanvasSettingsFromID(settings.CanvasID);

            Vector3 offset = new Vector3(0, -0.75f * (index * canvasSettings.DistanceModifier), 0); // Offset

            switch (position)
            {
                case CounterPositions.BelowCombo:
                    pos = new Vector3(-X, 1.15f - comboOffset, 0);
                    break;
                case CounterPositions.AboveCombo:
                    pos = new Vector3(-X, 2f + comboOffset, 0);
                    offset = new Vector3(0, (offset.y * -1) + 0.75f, 0);
                    break;
                case CounterPositions.BelowMultiplier:
                    pos = new Vector3(X, 1.05f - multOffset, 0);
                    break;
                case CounterPositions.AboveMultiplier:
                    pos = new Vector3(X, 2f + multOffset, 0);
                    offset = new Vector3(0, (offset.y * -1) + 0.75f, 0);
                    break;
                case CounterPositions.BelowEnergy:
                    pos = new Vector3(0, belowEnergyOffset, 0);
                    break;
                case CounterPositions.AboveHighway:
                    pos = new Vector3(0, 2.5f, 0);
                    offset = new Vector3(0, (offset.y * -1) + aboveHighwayOffset, 0);
                    break;
            }
            return pos + offset;
        }

        public void ClearAllText()
        {
            foreach (Canvas canvas in CanvasIDToCanvas.Values)
            {
                foreach (Transform child in canvas.transform)
                {
                    Object.Destroy(child.gameObject);
                }
            }
        }

        private void HideBaseGameHUDElement<T>(CoreGameHUDController coreGameHUD) where T : MonoBehaviour
        {
            GameObject gameObject = coreGameHUD.GetComponentInChildren<T>().gameObject;
            if (gameObject != null && gameObject.activeInHierarchy)
                RecurseFunctionOverGameObjectTree(gameObject, (child) => child.SetActive(false));
            gameObject.SetActive(false);
        }

        private void RecurseFunctionOverGameObjectTree(GameObject go, System.Action<GameObject> func)
        {
            foreach (Transform child in go.transform)
            {
                RecurseFunctionOverGameObjectTree(child.gameObject, func);
                func?.Invoke(child.gameObject);
            }
        }
    }
}
