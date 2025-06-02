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

            try
            {
                Console.WriteLine("BRLA to USDC Token Swap Application");
                Console.WriteLine("-----------------------------------");

                // Parse amount from command line or ask user
                decimal amountToSwap;
                if (args.Length > 0 && decimal.TryParse(args[0], out amountToSwap))
                {
                    // Use amount from command line
                }
                else
                {
                    Console.Write("Enter amount of BRLA to swap: ");
                    if (!decimal.TryParse(Console.ReadLine(), out amountToSwap))
                    {
                        Console.WriteLine("Invalid amount. Please enter a valid number.");
                        return;
                    }
                }

                Console.WriteLine($"Swapping {amountToSwap} BRLA to USDC...");

                // Execute the swap
                var result = await swapService.SwapBrlaToUsdcAsync(amountToSwap);

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
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}