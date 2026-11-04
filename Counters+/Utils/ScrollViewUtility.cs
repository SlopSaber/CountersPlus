using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Components.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace CountersPlus.Utils
{
    internal static class ScrollViewUtility
    {
        /// <summary>
        /// BSML scroll views size their content to its preferred width, so wide rows overflow and are clipped on
        /// both sides, while settings rows keep a fixed width that leaves a gap before the scrollbar. Settings
        /// panels instead lay their content out at the viewport's width with rows stretched across it.
        /// </summary>
        internal static void FitContentToViewportWidth(BSMLScrollView scrollView)
        {
            if (scrollView == null) return;
            RectTransform content = scrollView._contentRectTransform;
            if (content == null) return;

            if (content.TryGetComponent(out ContentSizeFitter fitter))
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            content.anchorMin = new Vector2(0, content.anchorMin.y);
            content.anchorMax = new Vector2(1, content.anchorMax.y);
            content.sizeDelta = new Vector2(0, content.sizeDelta.y);

            foreach (VerticalLayoutGroup layout in content.GetComponentsInChildren<VerticalLayoutGroup>(true))
            {
                layout.childControlWidth = true;
                layout.childForceExpandWidth = true;
                if (layout.TryGetComponent(out ContentSizeFitter nestedFitter))
                    nestedFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            }

            foreach (GenericSetting setting in content.GetComponentsInChildren<GenericSetting>(true))
            {
                if (setting.TryGetComponent(out LayoutElement element))
                    element.flexibleWidth = 1;
            }
        }
    }
}
