using System.Threading.Tasks;

namespace BrlaUsdcSwap.Services
{
    public interface ISwapService
    {
        Task<string> SwapBrlaToUsdcAsync(decimal amount);
        
        // New generic method to support bidirectional swaps
        Task<string> SwapTokensAsync(string sellTokenAddress, string buyTokenAddress, decimal amount);
    }
}