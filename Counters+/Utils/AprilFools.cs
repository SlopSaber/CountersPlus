using BeatSaberMarkupLanguage;
using CountersPlus.ConfigModels;
using CountersPlus.Counters.Interfaces;
using HarmonyLib;
using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Zenject;

namespace CountersPlus.Utils
{
    public class AprilFools : IInitializable, ITickable, IDisposable, INoteEventHandler
    {
        private float t = 0;

        private TMP_FontAsset mainFont = BeatSaberUI.MainTextFont;

        private byte originalItalicStyle = 0;
        private HarmonyLib.Harmony harmony;
        private bool appliedStyle;

        private IEnumerable<TMP_Text> allText = Enumerable.Empty<TMP_Text>();

        public void Initialize()
        {
            // BEHOLD! MY NO-NOITALICS-INATOR!!!
            var dummy = FontStyles.Normal;
            harmony = new HarmonyLib.Harmony("com.caeden117.countersplus.haha-april-fools-funny");
            harmony.Patch(typeof(TMP_Text).GetProperty("fontStyle").GetSetMethod(),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => Prefix(ref dummy)), int.MinValue));

            Plugin.Logger.Info("April Fools active.");
            originalItalicStyle = mainFont.italicStyle;
        }

        private static void Prefix(ref FontStyles value)
        {
            value |= FontStyles.Italic;
        }

        public void Dispose()
        {
            harmony?.UnpatchSelf();
            mainFont.italicStyle = originalItalicStyle;
        }

        public void Tick()
        {
            t += Time.deltaTime;
            byte italicStyle = (byte)Mathf.Clamp(Mathf.Abs(t / 5 * Mathf.Sin(t / 5) / 5), 0, byte.MaxValue);
            if (appliedStyle && mainFont.italicStyle == italicStyle) return;

            if (!allText.Any())
            {
                allText = Resources.FindObjectsOfTypeAll<CurvedTextMeshPro>().Where(x => x != null && x.isActiveAndEnabled);
            }

            mainFont.italicStyle = italicStyle;

            // Rebuild text only when its visible italic angle changes.
            foreach (var tmp in allText)
            {
                appliedStyle = true;
                tmp.fontStyle = tmp.fontStyle;
                tmp.font = mainFont;
                tmp.SetAllDirty(); // holy shit this is so dirty
            }
        }

        public void OnNoteCut(NoteData data, NoteCutInfo info)
        {
            if (!info.allIsOK) t += 30;
        }

        // EVEN BETTER: IF SOMEONE MISSES THEN TEXT BECOMES EVEN MORE ITALIC
        public void OnNoteMiss(NoteData data)
        {
            if (data.colorType != ColorType.None) t += 30;
        }
    }
}
