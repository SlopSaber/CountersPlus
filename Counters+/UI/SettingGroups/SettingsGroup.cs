using HMUI;
using UnityEngine;
using CountersPlus.Utils;
using BeatSaberMarkupLanguage.Components;
using System.Collections.Generic;
using System.Reflection;

namespace CountersPlus.UI.SettingGroups
{
    /// <summary>
    /// Helper class that makes it easy to dynamically the content of the bottom settings list.
    /// </summary>
    public abstract class SettingsGroup
    {
        private static readonly Dictionary<(Assembly, string), Sprite> sprites = new Dictionary<(Assembly, string), Sprite>();
        public abstract int NumberOfCells();

        public virtual CustomListTableData.CustomCellInfo CellInfoForIdx(int idx)
            => new CountersPlusListTableCell(idx, "Unimplemented", null, LoadSprite("MainSettings"));

        public abstract void OnCellSelect(TableView view, int idx);

        public virtual void OnDisable() { }

        public virtual void OnEnable() { }

        public virtual float GetSize() => 20f;

        public virtual int CellToSelect() => 0;

        protected Sprite LoadSprite(string name) => LoadSprite(typeof(SettingsGroup).Assembly, $"CountersPlus.UI.Images.{name}.png");

        protected Sprite LoadSprite(Assembly assembly, string resourcePath)
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
