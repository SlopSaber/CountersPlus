using BeatSaberMarkupLanguage;
using CountersPlus.ConfigModels;
using CountersPlus.UI.ViewControllers;
using CountersPlus.Utils;
using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRUIControls;
using Zenject;

namespace CountersPlus.UI.FlowCoordinators
{
    public class CountersPlusSettingsFlowCoordinator : FlowCoordinator
    {
        public readonly Vector3 MAIN_SCREEN_OFFSET = new Vector3(0, -4, 0);


        [Inject] public List<ConfigModel> AllConfigModels;
        [Inject] private CanvasUtility canvasUtility { get; set; }
        [Inject] private MockCounter mockCounter { get; set; }
        [Inject] private MenuTransitionsHelper menuTransitionsHelper { get; set; }
        [Inject] private MenuEnvironmentManager menuEnvironmentManager { get; set; }
        [Inject] private GameScenesManager gameScenesManager { get; set; }
        [Inject] private FadeInOutController fadeInOutController { get; set; }
        [Inject] private VRInputModule vrInputModule { get; set; }
        [Inject] private MenuShockwave menuShockwave { get; set; }
        [Inject] private PlayerDataModel playerDataModel { get; set; }
        [Inject] private MainFlowCoordinator mainFlowCoordinator { get; set; }

        [Inject] private CountersPlusCreditsViewController credits { get; set; }
        [Inject] private CountersPlusBlankViewController blank { get; set; }
        [Inject] private CountersPlusMainScreenNavigationController mainScreenNavigation { get; set; }
        [Inject] private CountersPlusSettingSectionSelectionViewController settingsSelection { get; set; }
        [Inject] private SettingsManager settingsManager { get; set; }
        [Inject] private SongPreviewPlayer songPreviewPlayer { get; set; }

        private bool hasTransitioned = false;

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            if (addedToHierarchy)
            {
                showBackButton = true;
                SetTitle("Counters+");
            }

            ProvideInitialViewControllers(mainScreenNavigation, credits, null, settingsSelection);

            RefreshAllMockCounters();
            LogPreviewState();
        }

        private void LogPreviewState()
        {
            var canvas = canvasUtility.GetCanvasFromID(-1);
            if (canvas == null)
            {
                Plugin.Logger.Warn("Menu preview main canvas is missing after activation.");
                return;
            }

            int layerMask = 1 << canvas.gameObject.layer;
            Plugin.Logger.Notice($"Menu preview canvas: scene={canvas.gameObject.scene.name}, active={canvas.gameObject.activeInHierarchy}, enabled={canvas.enabled}, layer={LayerMask.LayerToName(canvas.gameObject.layer)}, children={canvas.transform.childCount}, position={canvas.transform.position}");
            foreach (var camera in Camera.allCameras)
            {
                Plugin.Logger.Notice($"Menu preview camera: name={camera.name}, active={camera.isActiveAndEnabled}, rendersCanvasLayer={(camera.cullingMask & layerMask) != 0}, cullingMask={camera.cullingMask}");
            }
        }

        public void DoSceneTransition(Action callback = null)
        {
            if (hasTransitioned) return;

            hasTransitioned = true;

            // Make sure our menu persists through the transition
            gameScenesManager._neverUnloadScenes.Add("MenuCore");

            menuTransitionsHelper._tutorialScenesTransitionSetupData.Init(
                playerDataModel.playerData.playerSpecificSettings,
                new GameplayAdditionalInformation());

            menuEnvironmentManager.ShowEnvironmentType(MenuEnvironmentManager.MenuEnvironmentType.None);

            // We're actually transitioning to the Tutorial sequence, but disabling the tutorial itself from starting.
            gameScenesManager.PushScenes(menuTransitionsHelper._tutorialScenesTransitionSetupData, 0.25f, null, (_) =>
            {
                // god this makes me want to retire from beat saber modding
                static void DisableAllNonImportantObjects(Transform original, Transform source, string[] importantObjects)
                {
                    foreach (Transform child in source)
                    {
                        if (importantObjects.Contains(child.name))
                        {
                            Transform loopback = child;
                            while (loopback != original)
                            {
                                loopback.gameObject.SetActive(true);
                                loopback = loopback.parent;
                            }
                        }
                        else
                        {
                            child.gameObject.SetActive(false);
                            DisableAllNonImportantObjects(original, child, importantObjects);
                        }
                    }
                }


                // Disable the tutorial from actually starting. I dont think there's a better way to do this...
                Transform tutorial = GameObject.Find("TutorialGameplay").transform;

                // Gotta do some jank to re-activate the Root Container
                menuEnvironmentManager.transform.root.gameObject.SetActive(true);

                if (settingsManager.settings.quality.screenDisplacementEffects)
                {
                    // Disable menu shockwave to forget about rendering order problems
                    menuShockwave.gameObject.SetActive(false);
                }

                // Reset menu audio to original state.
                songPreviewPlayer.CrossfadeToDefault();

                // When not in FPFC, disable the Menu input, and re-enable the Tutorial menu input
                if (!Environment.GetCommandLineArgs().Any(x => x.Equals("fpfc", StringComparison.OrdinalIgnoreCase)) &&
                    !Resources.FindObjectsOfTypeAll<FirstPersonFlyingController>().Any(x => x.isActiveAndEnabled))
                {
                    vrInputModule.gameObject.SetActive(false);

                    DisableAllNonImportantObjects(tutorial, tutorial, new string[]
                    {
                            "EventSystem",
                            "ControllerLeft",
                            "ControllerRight"
                    });
                }
                else // If we're in FPFC, just disable everything as usual.
                {
                    DisableAllNonImportantObjects(tutorial, tutorial, Array.Empty<string>());
                    vrInputModule.enabled = true;
                }

                callback?.Invoke();
            });
        }

        public void RefreshAllMockCounters()
        {
            foreach (ConfigModel settings in AllConfigModels)
            {
                mockCounter.UpdateMockCounter(settings);
            }
        }

        public void SetRightViewController(ViewController controller) => SetRightScreenViewController(controller, ViewController.AnimationType.In);

        public void PushToMainScreen(ViewController controller)
        {
            mainScreenNavigation.AssignViewController(controller);
            //if (hasTransitioned)
            //{
            //    PresentViewController(controller);
            //}
        }

        public void PopFromMainScreen()
        {
            mainScreenNavigation.AssignViewController(blank);
            //PresentViewController(blank, null, ViewController.AnimationDirection.Horizontal, true);
        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            hasTransitioned = false;
            canvasUtility.ClearAllText();
            mainFlowCoordinator.DismissFlowCoordinator(this, TransitionToMenu, ViewController.AnimationDirection.Horizontal, true);
        }

        private void HandleBackButtonWasPressed() => BackButtonWasPressed(topViewController);

        private void TransitionToMenu()
        {
            vrInputModule.gameObject.SetActive(true);

            // This took a long time to figure out.
            if (settingsManager.settings.quality.screenDisplacementEffects)
            {
                menuShockwave.gameObject.SetActive(true);
            }

            // Return back to the main menu.
            gameScenesManager.PopScenes(0.25f, null, (_) =>
            {
                // Unmark these scenes as persistent so they won't bother us in-game.
                gameScenesManager._neverUnloadScenes.Remove("MenuCore");
                menuEnvironmentManager.ShowEnvironmentType(MenuEnvironmentManager.MenuEnvironmentType.Default);
                fadeInOutController.FadeIn();
                songPreviewPlayer.CrossfadeToDefault();
            });
        }
    }
}
