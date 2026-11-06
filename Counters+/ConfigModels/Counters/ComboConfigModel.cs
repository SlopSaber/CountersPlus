using IPA.Config.Stores.Attributes;

namespace CountersPlus.ConfigModels
{
    internal class ComboConfigModel : ConfigModel
    {
        [Ignore]
        public override string DisplayName => "Combo";

        internal const CounterPositions DefaultPosition = CounterPositions.AboveCombo;
        internal const float DefaultDistance = -1.49f;

        public override bool Enabled { get; set; } = true;
        [UseConverter]
        public override CounterPositions Position { get; set; } = DefaultPosition;
        public override float Distance { get; set; } = DefaultDistance;
    }
}
