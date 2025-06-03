using System.Threading.Tasks;

namespace BrlaUsdcSwap.Services
{
    public interface ISwapService
    {
        Task<string> SwapTokensAsync(string sellTokenAddress, string buyTokenAddress, decimal amount);
    }
}