using IPA.Config.Stores.Attributes;

namespace CountersPlus.ConfigModels
{
    internal class ComboConfigModel : ConfigModel
    {
        [Ignore]
        public override string DisplayName => "Combo";

        public override bool Enabled { get; set; } = false;
        [UseConverter]
        public override CounterPositions Position { get; set; } = CounterPositions.AboveCombo;
        public override float Distance { get; set; } = 0;
    }
}
