using BrlaUsdcSwap.Configuration;
using Microsoft.Extensions.Options;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Xml;
using System.Collections.Generic; // Add this using directive

namespace BrlaUsdcSwap.Services
{
    public class SwapService : ISwapService
    {
        private readonly IZeroExService _zeroExService;
        private readonly AppSettings _appSettings;
        private readonly Web3 _web3;
        private readonly Dictionary<string, string> _tokenAbis; // Add this dictionary

        // ERC20 approval function ABI
        [Function("approve")]
        private class ApproveFunction : FunctionMessage
        {
            [Parameter("address", "_spender", 1)]
            public string? Spender { get; set; }

            [Parameter("uint256", "_value", 2)]
            public BigInteger Value { get; set; }
        }

        public SwapService(IZeroExService zeroExService, IOptions<AppSettings> appSettings)
        {
            _zeroExService = zeroExService;
            _appSettings = appSettings.Value;

            // Create web3 instance with account from private key
            var account = new Account(_appSettings.PrivateKey, _appSettings.ChainId);
            _web3 = new Web3(account, _appSettings.PolygonRpcUrl);

            // Initialize the token ABIs dictionary
            _tokenAbis = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    _appSettings.BrlaTokenAddress,
                    @"[{""inputs"":[{""internalType"":""address"",""name"":""_logic"",""type"":""address""},{""internalType"":""bytes"",""name"":""_data"",""type"":""bytes""}],""stateMutability"":""payable"",""type"":""constructor""},{""anonymous"":false,""inputs"":[{""indexed"":false,""internalType"":""address"",""name"":""previousAdmin"",""type"":""address""},{""indexed"":false,""internalType"":""address"",""name"":""newAdmin"",""type"":""address""}],""name"":""AdminChanged"",""type"":""event""},{""anonymous"":false,""inputs"":[{""indexed"":true,""internalType"":""address"",""name"":""beacon"",""type"":""address""}],""name"":""BeaconUpgraded"",""type"":""event""},{""anonymous"":false,""inputs"":[{""indexed"":true,""internalType"":""address"",""name"":""implementation"",""type"":""address""}],""name"":""Upgraded"",""type"":""event""},{""stateMutability"":""payable"",""type"":""fallback""},{""stateMutability"":""payable"",""type"":""receive""},{""constant"":true,""inputs"":[{""name"":""_owner"",""type"":""address""},{""name"":""_spender"",""type"":""address""}],""name"":""allowance"",""outputs"":[{""name"":""remaining"",""type"":""uint256""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""_spender"",""type"":""address""},{""name"":""_value"",""type"":""uint256""}],""name"":""approve"",""outputs"":[{""name"":""success"",""type"":""bool""}],""type"":""function""}]"
                },
                {
                    _appSettings.UsdcTokenAddress,
                    @"[{""inputs"":[{""internalType"":""address"",""name"":""implementationContract"",""type"":""address""}],""stateMutability"":""nonpayable"",""type"":""constructor""},{""anonymous"":false,""inputs"":[{""indexed"":false,""internalType"":""address"",""name"":""previousAdmin"",""type"":""address""},{""indexed"":false,""internalType"":""address"",""name"":""newAdmin"",""type"":""address""}],""name"":""AdminChanged"",""type"":""event""},{""anonymous"":false,""inputs"":[{""indexed"":false,""internalType"":""address"",""name"":""implementation"",""type"":""address""}],""name"":""Upgraded"",""type"":""event""},{""stateMutability"":""payable"",""type"":""fallback""},{""inputs"":[],""name"":""admin"",""outputs"":[{""internalType"":""address"",""name"":"""",""type"":""address""}],""stateMutability"":""view"",""type"":""function""},{""inputs"":[{""internalType"":""address"",""name"":""newAdmin"",""type"":""address""}],""name"":""changeAdmin"",""outputs"":[],""stateMutability"":""nonpayable"",""type"":""function""},{""inputs"":[],""name"":""implementation"",""outputs"":[{""internalType"":""address"",""name"":"""",""type"":""address""}],""stateMutability"":""view"",""type"":""function""},{""inputs"":[{""internalType"":""address"",""name"":""newImplementation"",""type"":""address""}],""name"":""upgradeTo"",""outputs"":[],""stateMutability"":""nonpayable"",""type"":""function""},{""inputs"":[{""internalType"":""address"",""name"":""newImplementation"",""type"":""address""},{""internalType"":""bytes"",""name"":""data"",""type"":""bytes""}],""name"":""upgradeToAndCall"",""outputs"":[],""stateMutability"":""payable"",""type"":""function""},{""constant"":true,""inputs"":[{""name"":""_owner"",""type"":""address""},{""name"":""_spender"",""type"":""address""}],""name"":""allowance"",""outputs"":[{""name"":""remaining"",""type"":""uint256""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""_spender"",""type"":""address""},{""name"":""_value"",""type"":""uint256""}],""name"":""approve"",""outputs"":[{""name"":""success"",""type"":""bool""}],""type"":""function""}]"
                }
            };
        }
        
        public async Task<string> SwapTokensAsync(string sellTokenAddress, string buyTokenAddress, decimal amount)
        {
            // Get token names for better logging
            string sellTokenName = sellTokenAddress == _appSettings.BrlaTokenAddress ? "BRLA" : "USDC";
            string buyTokenName = buyTokenAddress == _appSettings.BrlaTokenAddress ? "BRLA" : "USDC";

            Console.WriteLine($"Swapping {amount} {sellTokenName} to {buyTokenName}");
            Console.WriteLine($"Using wallet address: {_web3.TransactionManager.Account.Address}");

            // 1. Get quote from 0x API
            var quote = await _zeroExService.GetSwapQuoteAsync(
                sellTokenAddress,
                buyTokenAddress,
                amount);

            Console.WriteLine($"RAW Quote: {JsonConvert.SerializeObject(quote, Newtonsoft.Json.Formatting.Indented)}");

            // Display quote information
            if (quote.Route?.Fills?.Count > 0)
            {
                Console.WriteLine($"Quote received from sources: {string.Join(", ", quote.Route.Fills.Select(f => f.Source))}");
            }

            Console.WriteLine($"Expected output: {quote.BuyAmount} {buyTokenName} for {quote.SellAmount} {sellTokenName}");

            // Rest of the swap logic continues as before...
            // 2. Check and approve allowance if needed
            var sellAmountWei = new BigInteger(decimal.Parse(quote.SellAmount));
            if (quote.Issues?.Allowance is not null)
            {
                await ApproveTokenSpendingAsync(sellTokenAddress, quote.Issues.Allowance.Spender, sellAmountWei);
            }

            // 3. Execute the swap transaction
            var txInput = new TransactionInput
            {
                From = _web3.TransactionManager.Account.Address,
                To = quote.Transaction.To,
                Data = quote.Transaction.Data,
                Value = new HexBigInteger(new BigInteger(decimal.Parse(quote.Transaction.Value))),
                Gas = new HexBigInteger(new BigInteger(decimal.Parse(quote.Transaction.Gas)) * 12 / 10), // Adding 20% buffer to gas estimate
                GasPrice = new HexBigInteger(new BigInteger(decimal.Parse(quote.Transaction.GasPrice)))
            };

            Console.WriteLine("Sending transaction...");
            var transactionHash = await _web3.Eth.TransactionManager.SendTransactionAsync(txInput);

            Console.WriteLine($"Transaction sent: {transactionHash}");
            Console.WriteLine("Waiting for transaction to be mined...");
            var receipt = await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);

            // Wait for receipt (optional, can be removed if you don't want to wait)
            while (receipt == null)
            {
                await Task.Delay(5000); // Check every 5 seconds
                receipt = await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            }

            if (receipt.Status.Value == 1)
            {
                Console.WriteLine("Transaction successful!");
            }
            else
            {
                throw new Exception("Transaction failed");
            }

            return transactionHash;
        }

        private async Task ApproveTokenSpendingAsync(string tokenAddress, string spenderAddress, BigInteger amount)
        {
            Console.WriteLine("Checking token allowance...");

            if (!_tokenAbis.TryGetValue(tokenAddress, out var tokenAbi))
            {
                throw new ArgumentException($"ABI not found for token address: {tokenAddress}");
            }

            // Create contract instance for the token
            var contract = _web3.Eth.GetContract(tokenAbi, tokenAddress);

            // Get allowance function
            var allowanceFunction = contract.GetFunction("allowance");
            var allowance = await allowanceFunction.CallAsync<BigInteger>(
                _web3.TransactionManager.Account.Address,
                spenderAddress);

            Console.WriteLine($"Current allowance: {allowance}");

            // If allowance is less than the amount we want to sell, approve
            if (allowance < amount)
            {
                Console.WriteLine("Approving token spending...");

                // Get approve function
                var approveFunction = contract.GetFunction("approve");
                string approveTxHash;

                try
                {
                    // Get current gas price
                    var gasPrice = await _web3.Eth.GasPrice.SendRequestAsync();
                    Console.WriteLine($"Current gas price: {gasPrice.Value} wei");

                    try
                    {
                        // Try to estimate gas first (for better accuracy)
                        var estimatedGas = await approveFunction.EstimateGasAsync(
                            _web3.TransactionManager.Account.Address,
                            null,
                            null,
                            spenderAddress,
                            amount);

                        // Add 30% buffer to the estimated gas
                        var gasLimit = new HexBigInteger(estimatedGas.Value * 13 / 10);

                        Console.WriteLine($"Estimated gas for approval: {estimatedGas.Value}");
                        Console.WriteLine($"Gas limit with buffer: {gasLimit.Value}");

                        // Send with explicit parameters
                        approveTxHash = await approveFunction.SendTransactionAsync(
                            _web3.TransactionManager.Account.Address,
                            gasLimit,
                            gasPrice,
                            null,
                            spenderAddress,
                            amount);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error during gas estimation: {ex.Message}");

                        // Fallback to fixed gas limit if estimation fails
                        Console.WriteLine("Using fallback fixed gas limit of 100000");
                        var gasLimit = new HexBigInteger(100000);

                        approveTxHash = await approveFunction.SendTransactionAsync(
                            _web3.TransactionManager.Account.Address,
                            gasLimit,
                            gasPrice,
                            null,
                            spenderAddress,
                            amount);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send approval transaction: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    }
                    throw new Exception("Token approval transaction failed to send", ex);
                }

                Console.WriteLine($"Approval transaction sent: {approveTxHash}");

                // Wait for the approval transaction to be mined
                var receipt = await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(approveTxHash);
                int attempts = 0;
                while (receipt == null && attempts < 30) // Limit to 30 attempts (2.5 minutes)
                {
                    await Task.Delay(5000); // Check every 5 seconds
                    receipt = await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(approveTxHash);
                    attempts++;
                    Console.WriteLine($"Waiting for approval tx receipt... Attempt {attempts}/30");
                }

                if (receipt == null)
                {
                    Console.WriteLine("Approval transaction is taking longer than expected to be mined.");
                    Console.WriteLine($"You can check the status manually: https://polygonscan.com/tx/{approveTxHash}");
                    throw new Exception("Approval transaction is taking too long to be mined.");
                }

                if (receipt.Status.Value != 1)
                {
                    Console.WriteLine($"Approval transaction failed with status: {receipt.Status.Value}");
                    Console.WriteLine($"Gas used: {receipt.GasUsed.Value}");
                    throw new Exception("Token approval failed on-chain");
                }

                Console.WriteLine($"Token approval successful. Gas used: {receipt.GasUsed.Value}");
            }
            else
            {
                Console.WriteLine("Token allowance is sufficient, no approval needed");
            }
        }
    }
}
