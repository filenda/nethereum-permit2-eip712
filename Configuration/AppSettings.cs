namespace BrlaUsdcSwap.Configuration
{
    public class AppSettings
    {
        public string ZeroExApiBaseUrl { get; set; }
        public string PolygonRpcUrl { get; set; }
        public string PrivateKey { get; set; }
        public string BrlaTokenAddress { get; set; }
        public string UsdcTokenAddress { get; set; }
        public string ZeroExApiKey { get; set; }
        public string ZeroExApiVer { get; set; }
        public int ChainId { get; set; }
        public string WalletAddress { get; set; }
        public int BrlaDecimals { get; set; } = 18;
        public int UsdcDecimals { get; set; } = 6;
    }
}