using System.Threading.Tasks;

namespace BrlaUsdcSwap.Services
{
    public interface ISwapService
    {
        Task<string> SwapBrlaToUsdcAsync(decimal amount);
    }
}