using Newtonsoft.Json;

namespace BrlaUsdcSwap.Models
{
    public class ZeroExQuoteResponse
    {
        [JsonProperty("price")]
        public string? Price { get; set; }

        [JsonProperty("guaranteedPrice")]
        public string? GuaranteedPrice { get; set; }

        [JsonProperty("to")]
        public string? To { get; set; }

        [JsonProperty("data")]
        public string? Data { get; set; }

        [JsonProperty("value")]
        public string? Value { get; set; }

        [JsonProperty("gas")]
        public string? Gas { get; set; }

        [JsonProperty("estimatedGas")]
        public string? EstimatedGas { get; set; }

        [JsonProperty("gasPrice")]
        public string? GasPrice { get; set; }

        [JsonProperty("protocolFee")]
        public string? ProtocolFee { get; set; }

        [JsonProperty("minimumProtocolFee")]
        public string? MinimumProtocolFee { get; set; }

        [JsonProperty("buyTokenAddress")]
        public string? BuyTokenAddress { get; set; }

        [JsonProperty("sellTokenAddress")]
        public string? SellTokenAddress { get; set; }

        [JsonProperty("buyAmount")]
        public string? BuyAmount { get; set; }

        [JsonProperty("sellAmount")]
        public string? SellAmount { get; set; }

        [JsonProperty("sources")]
        public List<Source>? Sources { get; set; }

        [JsonProperty("allowanceTarget")]
        public string? AllowanceTarget { get; set; }
    }

    public class Source
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("proportion")]
        public string? Proportion { get; set; }
    }
}