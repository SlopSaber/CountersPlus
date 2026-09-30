using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace CountersPlus.Utils
{
    // The game's late transparent pass includes NonReflectedParticles (19),
    // after screen displacement. Its ordinary transparent pass excludes it.
    internal sealed class CounterForegroundCanvas : MonoBehaviour
    {
        private readonly List<Graphic> graphics = new List<Graphic>();
        private int foregroundLayer;

        private void Awake()
        {
            foregroundLayer = LayerMask.NameToLayer("NonReflectedParticles");
            if (foregroundLayer < 0)
            {
                enabled = false;
                return;
            }
            gameObject.layer = foregroundLayer;
        }

        private void LateUpdate()
        {
            // Includes counters supplied by other mods and reparented score/rank
            // text. Reuse the list, and configure each graphic only once.
            GetComponentsInChildren(true, graphics);
            foreach (Graphic graphic in graphics)
            {
                graphic.gameObject.layer = foregroundLayer;
                if (graphic.GetComponent<CounterForegroundGraphic>() == null)
                    graphic.gameObject.AddComponent<CounterForegroundGraphic>();
            }
        }
    }

    internal sealed class CounterForegroundGraphic : MonoBehaviour
    {
        private static readonly int ZTestMode = Shader.PropertyToID("_ZTestMode");
        private static readonly int ZWrite = Shader.PropertyToID("_ZWrite");
        private Graphic graphic;
        private Material original;
        private Material owned;

        private void Awake()
        {
            graphic = GetComponent<Graphic>();
            RefreshMaterial();
        }

        private void LateUpdate() => RefreshMaterial();

        private void RefreshMaterial()
        {
            Material source = GetMaterial();
            if (source == null || source == owned)
                return;

            Material previous = owned;
            original = source;
            owned = new Material(source) { name = source.name + " (Counters+ foreground)" };
            owned.renderQueue = (int)RenderQueue.Overlay;
            if (owned.HasProperty(ZTestMode))
                owned.SetFloat(ZTestMode, (float)CompareFunction.Always);
            if (owned.HasProperty(ZWrite))
                owned.SetFloat(ZWrite, 0f);
            SetMaterial(owned);
            if (previous != null)
                Destroy(previous);
        }

        private Material GetMaterial() => graphic is TMP_Text text
            ? text.fontSharedMaterial : graphic.material;

        private void SetMaterial(Material material)
        {
            if (graphic is TMP_Text text)
                text.fontSharedMaterial = material;
            else
                graphic.material = material;
        }

        private void OnDestroy()
        {
            if (graphic != null && GetMaterial() == owned)
                SetMaterial(original);
            if (owned != null)
                Destroy(owned);
        }
    }
}
