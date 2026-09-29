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
        private readonly List<GameObject> objects = new List<GameObject>();

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

        public void Track(GameObject gameObject)
        {
            if (gameObject != null)
                objects.Add(gameObject);
        }

        internal void Clear()
        {
            foreach (GameObject gameObject in objects)
            {
                if (gameObject != null)
                    Object.Destroy(gameObject);
            }
            objects.Clear();
        }
    }
}
