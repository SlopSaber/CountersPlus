using IPA.Config.Stores.Attributes;

namespace CountersPlus.ConfigModels
{
    internal class MultiplierConfigModel : ConfigModel
    {
        [Ignore]
        public override string DisplayName => "Multiplier";

        internal const CounterPositions DefaultPosition = CounterPositions.AboveMultiplier;
        internal const float DefaultDistance = -1.93f;

        public override bool Enabled { get; set; } = true;
        [UseConverter]
        public override CounterPositions Position { get; set; } = DefaultPosition;
        public override float Distance { get; set; } = DefaultDistance;
    }
}
