using Newtonsoft.Json;
using System.Collections.Generic;

namespace BrlaUsdcSwap.Models
{
    public class ZeroExQuoteResponse
    {
        [JsonProperty("blockNumber")]
        public string? BlockNumber { get; set; }

        [JsonProperty("buyAmount")]
        public string? BuyAmount { get; set; }

        [JsonProperty("buyToken")]
        public string? BuyToken { get; set; }

        [JsonProperty("sellAmount")]
        public string? SellAmount { get; set; }

        [JsonProperty("sellToken")]
        public string? SellToken { get; set; }

        [JsonProperty("minBuyAmount")]
        public string? MinBuyAmount { get; set; }

        [JsonProperty("liquidityAvailable")]
        public bool LiquidityAvailable { get; set; }

        [JsonProperty("fees")]
        public Fees? Fees { get; set; }

        [JsonProperty("issues")]
        public Issues? Issues { get; set; }

        [JsonProperty("permit2")]
        public Permit2? Permit2 { get; set; }

        [JsonProperty("route")]
        public Route? Route { get; set; }

        [JsonProperty("tokenMetadata")]
        public TokenMetadata? TokenMetadata { get; set; }

        [JsonProperty("totalNetworkFee")]
        public string? TotalNetworkFee { get; set; }

        [JsonProperty("transaction")]
        public Transaction? Transaction { get; set; }

        [JsonProperty("zid")]
        public string? Zid { get; set; }
    }

    public class Fees
    {
        [JsonProperty("integratorFee")]
        public string? IntegratorFee { get; set; }

        [JsonProperty("zeroExFee")]
        public string? ZeroExFee { get; set; }

        [JsonProperty("gasFee")]
        public string? GasFee { get; set; }
    }

    public class Allowance
    {
        [JsonProperty("actual")]
        public string? Actual { get; set; }

        [JsonProperty("spender")]
        public string? Spender { get; set; }
    }

    public class Balance
    {
        [JsonProperty("token")]
        public string? Token { get; set; }

        [JsonProperty("actual")]
        public string? Actual { get; set; }

        [JsonProperty("expected")]
        public string? Expected { get; set; }
    }

    public class Issues
    {
        [JsonProperty("allowance")]
        public Allowance? Allowance { get; set; }

        [JsonProperty("balance")]
        public Balance? Balance { get; set; }

        [JsonProperty("simulationIncomplete")]
        public bool SimulationIncomplete { get; set; }

        [JsonProperty("invalidSourcesPassed")]
        public List<string>? InvalidSourcesPassed { get; set; }
    }

    public class TokenPermissions
    {
        [JsonProperty("token")]
        public string? Token { get; set; }

        [JsonProperty("amount")]
        public string? Amount { get; set; }
    }

    public class Eip712Domain
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("chainId")]
        public int ChainId { get; set; }

        [JsonProperty("verifyingContract")]
        public string? VerifyingContract { get; set; }
    }

    public class Eip712Message
    {
        [JsonProperty("permitted")]
        public TokenPermissions? Permitted { get; set; }

        [JsonProperty("spender")]
        public string? Spender { get; set; }

        [JsonProperty("nonce")]
        public string? Nonce { get; set; }

        [JsonProperty("deadline")]
        public string? Deadline { get; set; }
    }

    public class Eip712Type
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("type")]
        public string? Type { get; set; }
    }

    public class Eip712Types
    {
        [JsonProperty("PermitTransferFrom")]
        public List<Eip712Type>? PermitTransferFrom { get; set; }

        [JsonProperty("TokenPermissions")]
        public List<Eip712Type>? TokenPermissions { get; set; }

        [JsonProperty("EIP712Domain")]
        public List<Eip712Type>? EIP712Domain { get; set; }
    }

    public class Eip712
    {
        [JsonProperty("types")]
        public Eip712Types? Types { get; set; }

        [JsonProperty("domain")]
        public Eip712Domain? Domain { get; set; }

        [JsonProperty("message")]
        public Eip712Message? Message { get; set; }

        [JsonProperty("primaryType")]
        public string? PrimaryType { get; set; }
    }

    public class Permit2
    {
        [JsonProperty("type")]
        public string? Type { get; set; }

        [JsonProperty("hash")]
        public string? Hash { get; set; }

        [JsonProperty("eip712")]
        public Eip712? Eip712 { get; set; }
    }

    public class Fill
    {
        [JsonProperty("from")]
        public string? From { get; set; }

        [JsonProperty("to")]
        public string? To { get; set; }

        [JsonProperty("source")]
        public string? Source { get; set; }

        [JsonProperty("proportionBps")]
        public string? ProportionBps { get; set; }
    }

    public class Token
    {
        [JsonProperty("address")]
        public string? Address { get; set; }

        [JsonProperty("symbol")]
        public string? Symbol { get; set; }
    }

    public class Route
    {
        [JsonProperty("fills")]
        public List<Fill>? Fills { get; set; }

        [JsonProperty("tokens")]
        public List<Token>? Tokens { get; set; }
    }

    public class TokenTax
    {
        [JsonProperty("buyTaxBps")]
        public string? BuyTaxBps { get; set; }

        [JsonProperty("sellTaxBps")]
        public string? SellTaxBps { get; set; }
    }

    public class TokenMetadata
    {
        [JsonProperty("buyToken")]
        public TokenTax? BuyToken { get; set; }

        [JsonProperty("sellToken")]
        public TokenTax? SellToken { get; set; }
    }

    public class Transaction
    {
        [JsonProperty("to")]
        public string? To { get; set; }

        [JsonProperty("data")]
        public string? Data { get; set; }

        [JsonProperty("gas")]
        public string? Gas { get; set; }

        [JsonProperty("gasPrice")]
        public string? GasPrice { get; set; }

        [JsonProperty("value")]
        public string? Value { get; set; }
    }
}