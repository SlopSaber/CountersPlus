using IPA.Config.Stores.Attributes;

namespace CountersPlus.ConfigModels
{
    internal class MultiplayerRankConfigModel : ConfigModel
    {
        [Ignore]
        public override string DisplayName => "Multiplayer Rank";


        public override bool Enabled { get; set; } = false;

        [UseConverter]
        public override CounterPositions Position { get; set; } = CounterPositions.AboveHighway;
        
        public override float Distance { get; set; } = 0;
    }
}
