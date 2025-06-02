using BrlaUsdcSwap.Configuration;
using Microsoft.Extensions.Options;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace BrlaUsdcSwap.Services
{
    public class SwapService : ISwapService
    {
        private readonly IZeroExService _zeroExService;
        private readonly AppSettings _appSettings;
        private readonly Web3 _web3;

        // ERC20 approval function ABI
        [Function("approve")]
        private class ApproveFunction : FunctionMessage
        {
            [Parameter("address", "_spender", 1)]
            public string Spender { get; set; }

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
        }

        public async Task<string> SwapBrlaToUsdcAsync(decimal amount)
        {
            // 1. Get quote from 0x API
            var quote = await _zeroExService.GetSwapQuoteAsync(
                _appSettings.BrlaTokenAddress,
                _appSettings.UsdcTokenAddress,
                amount);

            Console.WriteLine($"Quote received: {quote.Price} USDC per BRLA");
            Console.WriteLine($"Expected output: {quote.BuyAmount} USDC");

            // 2. Check and approve allowance if needed
            var sellAmountWei = new BigInteger(decimal.Parse(quote.SellAmount));
            await ApproveTokenSpendingAsync(_appSettings.BrlaTokenAddress, quote.AllowanceTarget, sellAmountWei);

            // 3. Execute the swap transaction
            var txInput = new TransactionInput
            {
                From = _web3.TransactionManager.Account.Address,
                To = quote.To,
                Data = quote.Data,
                Value = new HexBigInteger(new BigInteger(decimal.Parse(quote.Value))),
                Gas = new HexBigInteger(new BigInteger(decimal.Parse(quote.Gas)) * 12 / 10), // Adding 20% buffer to gas estimate
                GasPrice = new HexBigInteger(new BigInteger(decimal.Parse(quote.GasPrice)))
            };

            Console.WriteLine("Sending transaction...");
            var transactionHash = await _web3.Eth.TransactionManager.SendTransactionAsync(txInput);
            
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
            
            // Create contract instance for the token
            var contract = _web3.Eth.GetContract(
                @"[{""constant"":true,""inputs"":[{""name"":""_owner"",""type"":""address""},{""name"":""_spender"",""type"":""address""}],""name"":""allowance"",""outputs"":[{""name"":""remaining"",""type"":""uint256""}],""type"":""function""},
                {""constant"":false,""inputs"":[{""name"":""_spender"",""type"":""address""},{""name"":""_value"",""type"":""uint256""}],""name"":""approve"",""outputs"":[{""name"":""success"",""type"":""bool""}],""type"":""function""}]",
                tokenAddress);

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
                
                // Approve the spender to spend tokens
                var approveFunction = contract.GetFunction("approve");
                var approveTxHash = await approveFunction.SendTransactionAsync(
                    _web3.TransactionManager.Account.Address,
                    null, // Gas
                    null, // Gas price
                    null, // Value
                    spenderAddress,
                    amount);

                Console.WriteLine($"Approval transaction sent: {approveTxHash}");

                // Wait for the approval transaction to be mined
                var receipt = await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(approveTxHash);
                while (receipt == null)
                {
                    await Task.Delay(5000); // Check every 5 seconds
                    receipt = await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(approveTxHash);
                }

                if (receipt.Status.Value != 1)
                {
                    throw new Exception("Token approval failed");
                }
                
                Console.WriteLine("Token approval successful");
            }
            else
            {
                Console.WriteLine("Token allowance is sufficient, no approval needed");
            }
        }
    }
}