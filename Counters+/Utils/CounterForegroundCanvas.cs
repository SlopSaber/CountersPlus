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
        private readonly Dictionary<Material, Material> pausedMaterials = new Dictionary<Material, Material>();
        private readonly Dictionary<Material, Material> originalMaterials = new Dictionary<Material, Material>();
        private int foregroundLayer;
        private int uiLayer;
        private IGamePause gamePause;
        private bool paused;
        private bool hierarchyDirty = true;
        private float nextDiscoveryTime;

        private void Awake()
        {
            foregroundLayer = LayerMask.NameToLayer("NonReflectedParticles");
            uiLayer = LayerMask.NameToLayer("UI");
            if (foregroundLayer < 0 || uiLayer < 0)
            {
                enabled = false;
                return;
            }
            gameObject.layer = foregroundLayer;
        }

        internal void Initialize(IGamePause pause)
        {
            if (foregroundLayer < 0 || uiLayer < 0) return;
            if (gamePause != null)
            {
                gamePause.didPauseEvent -= HandlePaused;
                gamePause.didResumeEvent -= HandleResumed;
            }
            gamePause = pause;
            if (gamePause != null)
            {
                gamePause.didPauseEvent += HandlePaused;
                gamePause.didResumeEvent += HandleResumed;
            }
            SetPaused(gamePause != null && gamePause.isPaused);
        }

        private void HandlePaused() => SetPaused(true);
        private void HandleResumed() => SetPaused(false);

        private void SetPaused(bool value)
        {
            if (paused == value) return;
            paused = value;
            RefreshHierarchy();
            foreach (Graphic graphic in graphics)
                if (graphic != null)
                    graphic.GetComponent<CounterForegroundGraphic>()?.RefreshForRenderMode();
        }

        internal void MarkHierarchyDirty() => hierarchyDirty = true;

        private void OnEnable() => MarkHierarchyDirty();

        private void OnTransformChildrenChanged() => MarkHierarchyDirty();

        private void LateUpdate()
        {
            // Replay transitions can set pause state without the normal events.
            bool currentPaused = gamePause != null && gamePause.isPaused;
            if (currentPaused != paused)
                SetPaused(currentPaused);
            // Direct children and TMP submeshes signal changes. A slow fallback
            // also discovers graphics added inside third-party nested groups.
            if (!hierarchyDirty && Time.unscaledTime < nextDiscoveryTime)
                return;
            RefreshHierarchy();
        }

        private void RefreshHierarchy()
        {
            hierarchyDirty = false;
            nextDiscoveryTime = Time.unscaledTime + 0.5f;
            int layer = paused ? uiLayer : foregroundLayer;
            if (gameObject.layer != layer)
                gameObject.layer = layer;

            GetComponentsInChildren(true, graphics);
            foreach (Graphic graphic in graphics)
            {
                if (graphic.gameObject.layer != layer)
                    graphic.gameObject.layer = layer;
                if (graphic.GetComponent<CounterForegroundGraphic>() == null)
                    graphic.gameObject.AddComponent<CounterForegroundGraphic>();
            }
        }

        internal Material GetForegroundMaterial(Material source, out Material original)
        {
            if (originalMaterials.TryGetValue(source, out Material basis) && basis != null)
                source = basis;
            original = source;
            // Paused HUD returns to the UI pass before the menu's transparent graphics.
            int queue = paused ? (int)RenderQueue.Transparent - 1 : (int)RenderQueue.Overlay;
            CompareFunction depthTest = paused ? CompareFunction.LessEqual : CompareFunction.Always;
            // TMP fallback materials inherit the parent's foreground settings.
            // Keep those borrowed materials rather than swapping them back and
            // forth with TMP's material reference on each text regeneration.
            if (source.renderQueue == queue &&
                (!source.HasProperty("_ZTestMode") || source.GetFloat("_ZTestMode") == (float)depthTest ||
                    (!paused && source.GetFloat("_ZTestMode") == (float)CompareFunction.Disabled)) &&
                (!source.HasProperty("_ZWrite") || source.GetFloat("_ZWrite") == 0f))
                return source;
            var materials = paused ? pausedMaterials : foregroundMaterials;
            if (materials.TryGetValue(source, out Material material))
                return material;

            material = new Material(source) { name = source.name + (paused ? " (Counters+ paused)" : " (Counters+ foreground)") };
            material.renderQueue = queue;
            if (material.HasProperty("_ZTestMode"))
                material.SetFloat("_ZTestMode", (float)depthTest);
            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", 0f);
            materials.Add(source, material);
            originalMaterials.Add(material, source);
            return material;
        }

        private void OnDestroy()
        {
            if (gamePause != null)
            {
                gamePause.didPauseEvent -= HandlePaused;
                gamePause.didResumeEvent -= HandleResumed;
            }
            foreach (Material material in foregroundMaterials.Values)
                if (material != null)
                    Destroy(material);
            foreach (Material material in pausedMaterials.Values)
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

        internal void RefreshForRenderMode()
        {
            foreground = null;
            RefreshMaterial();
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
