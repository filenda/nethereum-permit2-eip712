using BrlaUsdcSwap.Configuration;
using BrlaUsdcSwap.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace BrlaUsdcSwap.Services
{
    public class ZeroExService : IZeroExService
    {
        private readonly HttpClient _httpClient;
        private readonly AppSettings _appSettings;

        public ZeroExService(IHttpClientFactory httpClientFactory, IOptions<AppSettings> appSettings)
        {
            _httpClient = httpClientFactory.CreateClient();
            _appSettings = appSettings.Value;

            // Set base address and default headers
            _httpClient.BaseAddress = new Uri(_appSettings.ZeroExApiBaseUrl);

            if (!string.IsNullOrEmpty(_appSettings.ZeroExApiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("0x-api-key", _appSettings.ZeroExApiKey);
                _httpClient.DefaultRequestHeaders.Add("0x-version", _appSettings.ZeroExApiVer);
            }
        }

        public async Task<ZeroExQuoteResponse> GetSwapQuoteAsync(string sellToken, string buyToken, decimal sellAmount)
        {
            // Convert decimal to integer with 18 decimals (common for ERC20 tokens)
            // Note: BRLA and USDC might have different decimals, adjust as needed
            var sellAmountInWei = Convert.ToInt64(sellAmount * 1_000_000_000_000_000_000);

            // Build query parameters
            var queryParams = HttpUtility.ParseQueryString(string.Empty);
            queryParams["chainId"] = _appSettings.ChainId.ToString();
            queryParams["buyToken"] = buyToken;
            queryParams["sellToken"] = sellToken;
            queryParams["sellAmount"] = sellAmountInWei.ToString();
            var account = new Nethereum.Web3.Accounts.Account(_appSettings.PrivateKey, _appSettings.ChainId);
            var walletAddress = account.Address;
            queryParams["taker"] = walletAddress;
            // queryParams["slippagePercentage"] = "0.01"; // 1% slippage
            // queryParams["skipValidation"] = "true";
            // queryParams["enableSlippageProtection"] = "true";

            // Make the request
            var response = await _httpClient.GetAsync($"swap/permit2/quote?{queryParams}");
            response.EnsureSuccessStatusCode();

            // Parse the response
            var content = await response.Content.ReadAsStringAsync();
            var quote = JsonConvert.DeserializeObject<ZeroExQuoteResponse>(content);

            return quote;
        }
    }
}
