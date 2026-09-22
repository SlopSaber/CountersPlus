using CountersPlus.ConfigModels;
using Zenject;
using UnityEngine;

namespace CountersPlus.Counters
{
    internal class ComboCounter : Counter<ComboConfigModel>
    {
        [Inject] private CoreGameHUDController coreGameHUDController { get; set; }

        public override void CounterInit()
        {
            ComboUIController comboUIController = coreGameHUDController.GetComponentInChildren<ComboUIController>(true);
            MoveBaseGameHudElement(comboUIController.transform);
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
