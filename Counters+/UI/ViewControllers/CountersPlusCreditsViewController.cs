using System.Collections;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaberMarkupLanguage.Attributes;
using CountersPlus.ConfigModels;
using CountersPlus.UI.FlowCoordinators;
using CountersPlus.Utils;
using Zenject;

namespace CountersPlus.UI.ViewControllers
{
    class CountersPlusCreditsViewController : BSMLResourceViewController
    {
        private readonly string MainSettings = Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "CountersPlus.UI.BSML.MainSettings.bsml");

        [Inject] private VersionUtility versionUtility { get; set; }
        [Inject] private MainConfigModel mainConfig { get; set; }
        [Inject] private LazyInject<CountersPlusSettingsFlowCoordinator> flowCoordinator { get; set; }

        public override string ResourceName => "CountersPlus.UI.BSML.Credits.bsml";

        [UIComponent("versionText")] private TextMeshProUGUI version = null;
        [UIComponent("creatorText")] private TextMeshProUGUI creator = null;
        [UIComponent("updateText")] private TextMeshProUGUI update = null;
        [UIComponent("aprilFoolsText")] private TextMeshProUGUI aprilfools = null;
        [UIComponent("linkOpenedText")] private TextMeshProUGUI linkOpened = null;
        [UIComponent("github")] private Button github = null;
        [UIComponent("donate")] private Button donate = null;
        [UIComponent("issues")] private Button issues = null;
        [UIObject("main-settings")] private GameObject mainSettings = null;

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            mainConfig.OnConfigChanged += MainConfig_OnConfigChanged;
            if (!firstActivation) return;

            version.text = $"Version <color={(versionUtility.HasLatestVersion ? "#00FF00" : "#FF0000")}>{versionUtility.PluginVersion}</color>";
            creator.text = "Developed by: <color=#00c0ff>Caeden117</color>";
            update.text = $"<color=#FF0000>Version {versionUtility.BeatModsVersion} available!</color>";

            System.DateTime date = System.DateTime.Now;
            aprilfools.gameObject.SetActive(date.Month == 4 && date.Day == 1);

            if (versionUtility.HasLatestVersion) update.gameObject.SetActive(false);
            linkOpened.gameObject.SetActive(false);

            github.onClick.AddListener(() => GoTo("https://github.com/Caeden117/CountersPlus", github));
            issues.onClick.AddListener(() => GoTo("https://github.com/Caeden117/CountersPlus/issues", issues));
            donate.onClick.AddListener(() => GoTo("https://ko-fi.com/Caeden117", donate));

            BSMLParser.Instance.Parse(MainSettings, mainSettings, mainConfig);
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
            mainConfig.OnConfigChanged -= MainConfig_OnConfigChanged;
        }

        private void MainConfig_OnConfigChanged() => flowCoordinator.Value.RefreshAllMockCounters();

        private void GoTo(string url, Button button)
        {
            button.interactable = false;
            linkOpened.gameObject.SetActive(true);
            StartCoroutine(SecondRemove(button));
            Application.OpenURL(url);
        }

        private IEnumerator SecondRemove(Button button)
        {
            yield return new WaitForSeconds(5);
            linkOpened.gameObject.SetActive(false);
            button.interactable = true;
        }
    }
}
