namespace Lykke.HftApi.Domain.Entities
{
    public class AssetPair
    {
        public string AssetPairId { get; set; }
        public string BaseAssetId { get; set; }
        public string QuoteAssetId { get; set; }
        public string Name { get; set; }
        public int Accuracy { get; set; }
        public int InvertedAccuracy { get; set; }
        public decimal MinVolume { get; set; }
        public decimal MinInvertedVolume { get; set; }
    }
}