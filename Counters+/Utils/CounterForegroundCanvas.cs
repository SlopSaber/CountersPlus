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
        private readonly Dictionary<Material, Material> foregroundMaterials = new Dictionary<Material, Material>();
        private readonly Dictionary<Material, Material> originalMaterials = new Dictionary<Material, Material>();
        private int foregroundLayer;
        private bool hierarchyDirty = true;
        private float nextDiscoveryTime;

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

        internal void MarkHierarchyDirty() => hierarchyDirty = true;

        private void OnEnable() => MarkHierarchyDirty();

        private void OnTransformChildrenChanged() => MarkHierarchyDirty();

        private void LateUpdate()
        {
            // Direct children and TMP submeshes signal changes. A slow fallback
            // also discovers graphics added inside third-party nested groups.
            if (!hierarchyDirty && Time.unscaledTime < nextDiscoveryTime)
                return;
            hierarchyDirty = false;
            nextDiscoveryTime = Time.unscaledTime + 0.5f;

            GetComponentsInChildren(true, graphics);
            foreach (Graphic graphic in graphics)
            {
                if (graphic.gameObject.layer != foregroundLayer)
                    graphic.gameObject.layer = foregroundLayer;
                if (graphic.GetComponent<CounterForegroundGraphic>() == null)
                    graphic.gameObject.AddComponent<CounterForegroundGraphic>();
            }
        }

        internal Material GetForegroundMaterial(Material source, out Material original)
        {
            if (originalMaterials.TryGetValue(source, out original))
                return source;
            original = source;
            if (foregroundMaterials.TryGetValue(source, out Material material))
                return material;

            material = new Material(source) { name = source.name + " (Counters+ foreground)" };
            material.renderQueue = (int)RenderQueue.Overlay;
            if (material.HasProperty("_ZTestMode"))
                material.SetFloat("_ZTestMode", (float)CompareFunction.Always);
            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", 0f);
            foregroundMaterials.Add(source, material);
            originalMaterials.Add(material, source);
            return material;
        }

        private void OnDestroy()
        {
            foreach (Material material in foregroundMaterials.Values)
                if (material != null)
                    Destroy(material);
        }
    }

    internal sealed class CounterForegroundGraphic : MonoBehaviour
    {
        private Graphic graphic;
        private CounterForegroundCanvas canvas;
        private Material original;
        private Material foreground;
        private bool refreshing;

        private void Awake()
        {
            graphic = GetComponent<Graphic>();
            canvas = GetComponentInParent<CounterForegroundCanvas>();
            graphic.RegisterDirtyMaterialCallback(RefreshMaterial);
            RefreshMaterial();
        }

        private void OnTransformChildrenChanged()
        {
            if (canvas != null)
                canvas.MarkHierarchyDirty();
        }

        private void RefreshMaterial()
        {
            if (refreshing || canvas == null || graphic == null)
                return;
            Material source = GetMaterial();
            if (source == null || source == foreground)
                return;
            refreshing = true;
            try
            {
                foreground = canvas.GetForegroundMaterial(source, out original);
                if (source != foreground)
                    SetMaterial(foreground);
            }
            finally
            {
                refreshing = false;
            }
        }

        private Material GetMaterial() => graphic is TMP_Text text
            ? text.fontSharedMaterial
            // TMP_SubMeshUI.material calls GetMaterial(), which dirties vertices
            // and material on every read. Its shared getter has no side effects.
            : graphic is TMP_SubMeshUI subMesh ? subMesh.sharedMaterial : graphic.material;

        private void SetMaterial(Material material)
        {
            if (graphic is TMP_Text text)
                text.fontSharedMaterial = material;
            else if (graphic is TMP_SubMeshUI subMesh)
                subMesh.sharedMaterial = material;
            else
                graphic.material = material;
        }

        private void OnDestroy()
        {
            if (graphic != null)
            {
                graphic.UnregisterDirtyMaterialCallback(RefreshMaterial);
                if (GetMaterial() == foreground)
                    SetMaterial(original);
            }
        }
    }
}
