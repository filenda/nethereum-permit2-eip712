using BrlaUsdcSwap.Models;
using System.Threading.Tasks;

namespace BrlaUsdcSwap.Services
{
    public interface IZeroExService
    {
        Task<ZeroExQuoteResponse> GetSwapQuoteAsync(string sellToken, string buyToken, decimal sellAmount);
    }
}