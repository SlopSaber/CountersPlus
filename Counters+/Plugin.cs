using CountersPlus.Custom;
using CountersPlus.ConfigModels;
using CountersPlus.Installers;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using SiraUtil.Zenject;
using System.Collections.Generic;
using System.Reflection;
using IPALogger = IPA.Logging.Logger;
using HarmonyObj = HarmonyLib.Harmony;

namespace CountersPlus
{
    [Plugin(RuntimeOptions.DynamicInit)]
    public class Plugin
    {
        internal static Plugin Instance { get; private set; }
        internal static IPALogger Logger { get; private set; }
        internal static MainConfigModel MainConfig { get; private set; }
        internal static List<CustomCounter> LoadedCustomCounters { get; private set; } = new List<CustomCounter>();

        private const string HARMONY_ID = "com.caeden117.countersplus";
        private HarmonyObj harmony;

        [Init]
        public Plugin(IPALogger logger,
            [Config.Name("CountersPlus")] Config conf,
            Zenjector zenjector)
        {
            Instance = this;
            Logger = logger;
            MainConfig = conf.Generated<MainConfigModel>();
            MigrateBaseHudToggles(MainConfig);
            harmony = new HarmonyObj(HARMONY_ID);

            zenjector.Expose<CoreGameHUDController>(condition: (context, _) =>
                context is Zenject.SceneDecoratorContext decorator && decorator.DecoratedContractName == "Environment");

            zenjector.Install<CoreInstaller>(Location.App);
            zenjector.Install<MenuUIInstaller>(Location.Menu);

            zenjector.Install<CountersInstaller>(Location.StandardPlayer | Location.CampaignPlayer);
            zenjector.Install<MultiplayerCountersInstaller, MultiplayerLocalActivePlayerInstaller>();
        }

        // The combo and multiplier used to stay in place while their counters were disabled, unless hidden separately.
        // Disabling those counters now hides them, so keep previously visible ones enabled at the base game position.
        private static void MigrateBaseHudToggles(MainConfigModel config)
        {
            if (config.BaseHudTogglesMigrated) return;

            if (!config.ComboConfig.Enabled && !config.HideCombo)
            {
                config.ComboConfig.Enabled = true;
                config.ComboConfig.Position = ComboConfigModel.DefaultPosition;
                config.ComboConfig.Distance = ComboConfigModel.DefaultDistance;
            }
            if (!config.MultiplierConfig.Enabled && !config.HideMultiplier)
            {
                config.MultiplierConfig.Enabled = true;
                config.MultiplierConfig.Position = MultiplierConfigModel.DefaultPosition;
                config.MultiplierConfig.Distance = MultiplierConfigModel.DefaultDistance;
            }
            config.BaseHudTogglesMigrated = true;
            config.Changed();
        }

        [OnEnable]
        public void OnEnable() => harmony.PatchAll(Assembly.GetExecutingAssembly());

        [OnDisable]
        public void OnDisable() => harmony.UnpatchSelf();
    }
}
