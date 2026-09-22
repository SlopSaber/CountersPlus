using CountersPlus.ConfigModels;
using TMPro;
using Zenject;
using UnityEngine;

namespace CountersPlus.Counters
{
    internal class MultiplierCounter : Counter<MultiplierConfigModel>
    {
        [Inject] private CoreGameHUDController coreGameHUDController { get; set; }
        [Inject] private ScoreController scoreController { get; set; }

        private TMP_Text multiplierText;

        public override void CounterInit()
        {
            ScoreMultiplierUIController multiplierUIController = coreGameHUDController.GetComponentInChildren<ScoreMultiplierUIController>(true);
            foreach (TMP_Text baseGameText in multiplierUIController.GetComponentsInChildren<TMP_Text>(true))
            {
                baseGameText.enabled = false;
            }

            MoveBaseGameHudElement(multiplierUIController.transform);

            multiplierText = CanvasUtility.CreateTextFromSettings(Settings);
            multiplierText.text = "x1";
            multiplierText.fontSize = 4;
            multiplierText.fontStyle = FontStyles.Italic;
            multiplierText.alignment = TextAlignmentOptions.Center;

            scoreController.multiplierDidChangeEvent += MultiplierDidChange;
        }

        private void MultiplierDidChange(int multiplier, float progress)
        {
            multiplierText.text = $"x{multiplier}";
        }

        public override void CounterDestroy()
        {
            scoreController.multiplierDidChangeEvent -= MultiplierDidChange;
        }

        private void MoveBaseGameHudElement(Transform transform)
        {
            SetActiveRecursively(transform.gameObject, true);

            Canvas currentCanvas = CanvasUtility.GetCanvasFromID(Settings.CanvasID);
            HUDCanvas currentSettings = CanvasUtility.GetCanvasSettingsFromCanvas(currentCanvas);
            float positionScale = currentSettings?.PositionScale ?? 10;

            transform.SetParent(currentCanvas.transform, true);
            transform.localPosition = CanvasUtility.GetAnchoredPositionFromConfig(Settings) * positionScale;
            transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0);
            transform.localEulerAngles = Vector3.zero;
        }

        private void SetActiveRecursively(GameObject gameObject, bool active)
        {
            gameObject.SetActive(active);
            foreach (Transform child in gameObject.transform)
            {
                SetActiveRecursively(child.gameObject, active);
            }
        }
    }
}
