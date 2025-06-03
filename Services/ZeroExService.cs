using BrlaUsdcSwap.Configuration;
using BrlaUsdcSwap.Models;
using Microsoft.Extensions.Options;
using Nethereum.Web3;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Numerics;
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
            // Determine correct decimals based on which token is being sold
            int decimals = sellToken == _appSettings.BrlaTokenAddress
                ? _appSettings.BrlaDecimals
                : _appSettings.UsdcDecimals;

            // Convert decimal to integer with appropriate decimals
            BigInteger sellAmountInWei;
            if (decimals == 18)
            {
                sellAmountInWei = Web3.Convert.ToWei(sellAmount);
            }
            else if (decimals == 6)
            {
                sellAmountInWei = new BigInteger(decimal.Truncate(sellAmount * 1_000_000m));
            }
            else
            {
                sellAmountInWei = (BigInteger)(sellAmount * (decimal)BigInteger.Pow(10, decimals));
            }

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
