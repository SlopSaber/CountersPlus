using CountersPlus.ConfigModels;
using CountersPlus.Custom;
using CountersPlus.Utils;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Zenject;

namespace CountersPlus.UI
{
    public class MockCounter
    {
        private readonly Dictionary<ConfigModel, CounterPreviewContext> activePreviews = new Dictionary<ConfigModel, CounterPreviewContext>();

        [Inject] private CanvasUtility canvasUtility { get; set; }
        [Inject] private MainConfigModel mainConfig { get; set; }
        [Inject] private DiContainer container { get; set; }

        private ConfigModel highlightedConfig;
        private TMP_Text selectionMarker;

        public void UpdateMockCounter(ConfigModel settings)
        {
            if (activePreviews.TryGetValue(settings, out CounterPreviewContext old))
            {
                old.Clear();
                activePreviews.Remove(settings);
            }

            if (settings.Enabled)
            {
                CounterPreviewContext preview = new CounterPreviewContext(canvasUtility, settings);
                try
                {
                    if (settings is CustomConfigModel custom)
                    {
                        Type previewType = custom.AttachedCustomCounter.PreviewType;
                        if (previewType == null)
                            preview.CreateText().text = custom.AttachedCustomCounter.Name;
                        else
                            ((ICounterPreview)container.Instantiate(previewType)).Render(preview);
                    }
                    else
                        BuiltInCounterPreview.Render(preview, mainConfig);
                }
                catch (Exception e)
                {
                    Plugin.Logger.Error($"Could not preview {settings.DisplayName}: {e}");
                    preview.Clear();
                    preview = new CounterPreviewContext(canvasUtility, settings);
                    preview.CreateText().text = settings.DisplayName;
                }
                activePreviews.Add(settings, preview);
            }

            if (highlightedConfig == settings)
                RefreshSelectionMarker();
        }

        public void HighlightCounter(ConfigModel settings)
        {
            highlightedConfig = settings;
            RefreshSelectionMarker();
        }

        private void RefreshSelectionMarker()
        {
            if (selectionMarker != null)
                UnityEngine.Object.Destroy(selectionMarker.gameObject);
            selectionMarker = null;

            if (highlightedConfig == null || !activePreviews.ContainsKey(highlightedConfig))
                return;

            selectionMarker = canvasUtility.CreateTextFromSettings(highlightedConfig, new Vector3(-1.4f, 0, 0));
            selectionMarker.text = "▶";
            selectionMarker.fontSize = 2;
            selectionMarker.color = Color.yellow;
        }
    }
}
