using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.ViewControllers;
using CountersPlus.ConfigModels;
using CountersPlus.UI.FlowCoordinators;
using CountersPlus.UI.ViewControllers.Editing;
using CountersPlus.Utils;
using HMUI;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace CountersPlus.UI.ViewControllers
{
    class CountersPlusHUDListViewController : BSMLResourceViewController
    {
        public override string ResourceName => "CountersPlus.UI.BSML.HUDs.HUDList.bsml";

        public int? SelectedCanvas { get; private set; }
        private Sprite blankSprite;

        [UIComponent("list")] private CustomListTableData data { get; set; }
        [UIComponent("new-canvas-name")] private ModalKeyboard newCanvasKeyboard { get; set; }
        [UIComponent("delete-canvas")] private ModalView deleteCanvas { get; set; }
        [UIComponent("canvas-error")] private ModalView canvasError { get; set; }
        [UIComponent("edit-button")] private Button editButton { get; set; }
        [UIComponent("delete-button")] private Button deleteButton { get; set; }
        [UIParams] private BSMLParserParams parserParams = null;

        [Inject] private HUDConfigModel hudConfig { get; set; }
        [Inject] private MainConfigModel mainConfig { get; set; }
        [Inject] private CanvasUtility canvasUtility { get; set; }
        [Inject] private LazyInject<CountersPlusSettingsFlowCoordinator> flowCoordinator { get; set; }
        [Inject] private LazyInject<CountersPlusHUDEditViewController> hudEdit { get; set; }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            RefreshData();
            ClearSelection();
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemEnabling)
        {
            DeactivateModals();
        }

        public void RefreshData()
        {
            if (blankSprite == null) blankSprite = Utilities.LoadSpriteFromTexture(Texture2D.blackTexture);
            data.Data.Clear();
            for (int i = -1; i < hudConfig.OtherCanvasSettings.Count; i++)
            {
                HUDCanvas settings = canvasUtility.GetCanvasSettingsFromID(i);
                int countersUsingCanvas = flowCoordinator.Value.AllConfigModels.Count(x => x.CanvasID == i);
                var info = new CustomListTableData.CustomCellInfo(
                    settings?.Name ?? "Unknown",
                    $"{countersUsingCanvas} counter(s) use this Canvas.", blankSprite);
                data.Data.Add(info);
            }
            data.TableView.ReloadData();
            if (SelectedCanvas.HasValue && SelectedCanvas.Value + 1 < data.Data.Count)
                data.TableView.SelectCellWithIdx(SelectedCanvas.Value + 1, false);
            else
                SelectedCanvas = null;
            RefreshButtons();
        }

        protected override void OnDestroy()
        {
            if (blankSprite != null) Destroy(blankSprite);
            base.OnDestroy();
        }

        public void DeactivateModals() => parserParams.EmitEvent("on-deactivate");

        public void ClearSelection()
        {
            SelectedCanvas = null;
            data.TableView.ClearSelection();
            RefreshButtons();
        }

        private void RefreshButtons()
        {
            editButton.interactable = SelectedCanvas.HasValue;
            deleteButton.interactable = SelectedCanvas.HasValue;
        }

        [UIAction("cell-selected")]
        private void CellSelected(TableView view, int idx)
        {
            SelectedCanvas = idx - 1;
            RefreshButtons();
        }

        [UIAction("new-canvas")]
        private void NewCanvas()
        {
            DeactivateModals();
            newCanvasKeyboard.ModalView.Show(true);
        }

        [UIAction("edit-canvas")]
        private void EditCanvas()
        {
            if (!SelectedCanvas.HasValue) return;
            DeactivateModals();
            int canvas = SelectedCanvas.Value;
            flowCoordinator.Value.SetRightViewController(hudEdit.Value);
            hudEdit.Value.ApplyCanvasForEditing(canvas);
        }

        [UIAction("delete-canvas-prompt")]
        private void DeleteCanvasPrompt()
        {
            if (!SelectedCanvas.HasValue) return;
            DeactivateModals();
            if (SelectedCanvas.Value == -1) canvasError.Show(true);
            else deleteCanvas.Show(true);
        }

        [UIAction("create-new-canvas")]
        private void CreateNewCanvas(string name)
        {
            HUDCanvas settings = new HUDCanvas();
            if (string.IsNullOrWhiteSpace(name)) name = "New Canvas";
            settings.Name = name;
            canvasUtility.RegisterNewCanvas(settings, hudConfig.OtherCanvasSettings.Count);
            hudConfig.OtherCanvasSettings.Add(settings);
            mainConfig.HUDConfig = hudConfig;
            SelectedCanvas = hudConfig.OtherCanvasSettings.Count - 1;
            RefreshData();
        }

        [UIAction("delete-selected-canvas")]
        private void DeleteSelectedCanvas()
        {
            DeactivateModals();
            if (!SelectedCanvas.HasValue || SelectedCanvas.Value == -1) return;
            int canvas = SelectedCanvas.Value;
            foreach (ConfigModel model in flowCoordinator.Value.AllConfigModels.Where(x => x.CanvasID == canvas).ToList())
                model.CanvasID = -1;
            canvasUtility.UnregisterCanvas(canvas);
            hudConfig.OtherCanvasSettings.RemoveAt(canvas);
            mainConfig.HUDConfig = hudConfig;
            SelectedCanvas = null;
            RefreshData();
            flowCoordinator.Value.RefreshAllMockCounters();
        }

        [UIAction("cancel-deletion")]
        private void CancelCanvasDeletion() => DeactivateModals();
    }
}
