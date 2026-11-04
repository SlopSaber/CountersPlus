using CountersPlus.ConfigModels;
using CountersPlus.Custom;
using CountersPlus.UI.FlowCoordinators;
using CountersPlus.UI.ViewControllers;
using CountersPlus.UI.ViewControllers.Editing;
using CountersPlus.Utils;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Zenject;

namespace CountersPlus.UI
{
    /// <summary>
    /// The entries shown in the counter selection strip and grid. Entry 0 opens the HUD canvas manager;
    /// the remaining entries are the loaded counters in order.
    /// </summary>
    public class CounterCatalog
    {
        public const int HUDsIndex = 0;

        private static readonly Dictionary<(Assembly, string), Sprite> sprites = new Dictionary<(Assembly, string), Sprite>();

        [Inject] private LazyInject<CountersPlusSettingsFlowCoordinator> flowCoordinator { get; set; }
        [Inject] private LazyInject<CountersPlusCounterEditViewController> editViewController { get; set; }
        [Inject] private LazyInject<CountersPlusHUDListViewController> hudList { get; set; }
        [Inject] private MockCounter mockCounter { get; set; }

        public int SelectedIndex { get; private set; } = -1;

        public int Count => flowCoordinator.Value.AllConfigModels.Count + 1;

        public CountersPlusListTableCell CellInfo(int idx)
        {
            if (idx == HUDsIndex)
                return new CountersPlusListTableCell(idx, "HUDs", null, LoadSprite("HUDs.Manage"), true);

            ConfigModel model = flowCoordinator.Value.AllConfigModels[idx - 1];
            if (model is CustomConfigModel customConfig)
            {
                CustomCounter counter = customConfig.AttachedCustomCounter;
                if (counter.BSML != null && !string.IsNullOrEmpty(counter.BSML.Icon))
                {
                    try
                    {
                        return new CountersPlusListTableCell(idx, counter.Name, null,
                            LoadSprite(counter.CounterType.Assembly, counter.BSML.Icon));
                    }
                    catch
                    {
                        return new CountersPlusListTableCell(idx, counter.Name, "\n<i>(Failed to load custom icon.)</i>", LoadSprite("Counters.Custom"));
                    }
                }
                return new CountersPlusListTableCell(idx, counter.Name, null, LoadSprite("Counters.Custom"));
            }
            return new CountersPlusListTableCell(idx, model.DisplayName, null, LoadSprite($"Counters.{model.DisplayName}"));
        }

        public void Select(int idx)
        {
            SelectedIndex = idx;
            if (idx == HUDsIndex)
            {
                mockCounter.HighlightCounter(null);
                flowCoordinator.Value.SetRightViewController(hudList.Value);
                return;
            }

            flowCoordinator.Value.SetRightViewController(editViewController.Value);
            editViewController.Value.ApplySettings(flowCoordinator.Value.AllConfigModels[idx - 1]);
        }

        public void ClearSelection()
        {
            SelectedIndex = -1;
            mockCounter.HighlightCounter(null);
        }

        private static Sprite LoadSprite(string name)
            => LoadSprite(typeof(CounterCatalog).Assembly, $"CountersPlus.UI.Images.{name}.png");

        private static Sprite LoadSprite(Assembly assembly, string resourcePath)
        {
            var key = (assembly, resourcePath);
            if (!sprites.TryGetValue(key, out Sprite sprite) || sprite == null)
            {
                sprite = ImagesUtility.LoadSpriteFromExternalAssemblyResources(assembly, resourcePath);
                sprites[key] = sprite;
            }
            return sprite;
        }
    }
}
