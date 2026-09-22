using IPA.Config.Stores.Attributes;

namespace CountersPlus.ConfigModels
{
    internal class MultiplierConfigModel : ConfigModel
    {
        [Ignore]
        public override string DisplayName => "Multiplier";

        public override bool Enabled { get; set; } = false;
        [UseConverter]
        public override CounterPositions Position { get; set; } = CounterPositions.AboveMultiplier;
        public override float Distance { get; set; } = 0;
    }
}
