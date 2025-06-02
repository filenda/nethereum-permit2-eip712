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
        public int ChainId { get; set; }
    }
}