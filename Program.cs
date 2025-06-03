using BrlaUsdcSwap.Configuration;
using BrlaUsdcSwap.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BrlaUsdcSwap
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            // Setup DI
            var serviceProvider = new ServiceCollection()
                .Configure<AppSettings>(configuration.GetSection("AppSettings"))
                .AddSingleton<IConfiguration>(configuration)
                .AddHttpClient()
                .AddTransient<IZeroExService, ZeroExService>()
                .AddTransient<ISwapService, SwapService>()
                .BuildServiceProvider();

            // Get services
            var swapService = serviceProvider.GetRequiredService<ISwapService>();
            var appSettings = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AppSettings>>().Value;

            try
            {
                Console.WriteLine("Token Swap Application");
                Console.WriteLine("-----------------------------------");
                Console.WriteLine("1. Swap BRLA to USDC");
                Console.WriteLine("2. Swap USDC to BRLA");

                // Get swap direction
                int swapDirection = 1; // Default to BRLA → USDC
                
                if (args.Length > 1 && int.TryParse(args[1], out int argDirection) && (argDirection == 1 || argDirection == 2))
                {
                    swapDirection = argDirection;
                }
                else
                {
                    Console.Write("Enter your choice (1 or 2): ");
                    if (!int.TryParse(Console.ReadLine(), out swapDirection) || (swapDirection != 1 && swapDirection != 2))
                    {
                        Console.WriteLine("Invalid choice. Using default: 1 (BRLA to USDC)");
                        swapDirection = 1;
                    }
                }

                // Determine token addresses based on swap direction
                string sellTokenAddress = swapDirection == 1 ? appSettings.BrlaTokenAddress : appSettings.UsdcTokenAddress;
                string buyTokenAddress = swapDirection == 1 ? appSettings.UsdcTokenAddress : appSettings.BrlaTokenAddress;
                string sellTokenName = swapDirection == 1 ? "BRLA" : "USDC";
                string buyTokenName = swapDirection == 1 ? "USDC" : "BRLA";

                // Parse amount from command line or ask user
                decimal amountToSwap;
                if (args.Length > 0 && decimal.TryParse(args[0], out amountToSwap))
                {
                    // Use amount from command line
                }
                else
                {
                    Console.Write($"Enter amount of {sellTokenName} to swap: ");
                    if (!decimal.TryParse(Console.ReadLine(), out amountToSwap))
                    {
                        Console.WriteLine("Invalid amount. Please enter a valid number.");
                        return;
                    }
                }

                Console.WriteLine($"Swapping {amountToSwap} {sellTokenName} to {buyTokenName}...");

                // Execute the swap
                var result = await swapService.SwapTokensAsync(sellTokenAddress, buyTokenAddress, amountToSwap);
                
                // Display result
                Console.WriteLine("Swap completed successfully!");
                Console.WriteLine($"Transaction Hash: {result}");
                Console.WriteLine("Check the transaction on PolygonScan:");
                Console.WriteLine($"https://polygonscan.com/tx/{result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Error: {ex.InnerException.Message}");
                }
                
                // Additional error details
                Console.WriteLine("\nTroubleshooting tips:");
                Console.WriteLine("1. Check your token balance");
                Console.WriteLine("2. Ensure you have enough MATIC for gas fees");
                Console.WriteLine("3. Try with a smaller amount");
                Console.WriteLine("4. Try increasing slippage tolerance in the settings");
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}