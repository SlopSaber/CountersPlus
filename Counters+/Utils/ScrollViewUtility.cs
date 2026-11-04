using BeatSaberMarkupLanguage.Components;
using UnityEngine;
using UnityEngine.UI;

namespace CountersPlus.Utils
{
    internal static class ScrollViewUtility
    {
        /// <summary>
        /// BSML scroll views size their content to its preferred width, so wide rows overflow and are clipped on
        /// both sides. Settings panels instead lay their content out at the viewport's width.
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

            if (content.TryGetComponent(out VerticalLayoutGroup layout))
            {
                layout.childControlWidth = true;
                layout.childForceExpandWidth = true;
            }
            foreach (Transform child in content)
            {
                if (child.TryGetComponent(out VerticalLayoutGroup childLayout))
                {
                    childLayout.childControlWidth = true;
                    childLayout.childForceExpandWidth = true;
                }
            }
        }
    }
}
