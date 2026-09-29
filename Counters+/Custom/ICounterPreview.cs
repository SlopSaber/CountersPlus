using CountersPlus.ConfigModels;
using CountersPlus.Utils;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CountersPlus.Custom
{
    public interface ICounterPreview
    {
        void Render(CounterPreviewContext context);
    }

    public sealed class CounterPreviewContext
    {
        private readonly List<UnityEngine.Object> objects = new List<UnityEngine.Object>();

        public CanvasUtility CanvasUtility { get; }
        public ConfigModel Settings { get; }

        public CounterPreviewContext(CanvasUtility canvasUtility, ConfigModel settings)
        {
            CanvasUtility = canvasUtility;
            Settings = settings;
        }

        public TMP_Text CreateText(Vector3? offset = null) => CreateText(Settings, offset);

        public TMP_Text CreateText(ConfigModel settings, Vector3? offset = null)
        {
            TMP_Text text = CanvasUtility.CreateTextFromSettings(settings, offset);
            Track(text.gameObject);
            return text;
        }

        public void Track(UnityEngine.Object resource)
        {
            if (resource != null)
                objects.Add(resource);
        }

        internal void Clear()
        {
            foreach (UnityEngine.Object resource in objects)
            {
                if (resource != null)
                    Object.Destroy(resource);
            }
            objects.Clear();
        }
    }
}
