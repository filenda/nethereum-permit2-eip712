using BrlaUsdcSwap.Configuration;
using Microsoft.Extensions.Options;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Collections.Generic;
using Nethereum.Signer;
using Nethereum.Signer.EIP712;
using Nethereum.Util;
using BrlaUsdcSwap.Models;
using Nethereum.ABI.EIP712;
using Nethereum.Hex.HexConvertors.Extensions;

namespace BrlaUsdcSwap.Services
{
    public class SwapService : ISwapService
    {
        private readonly IZeroExService _zeroExService;
        private readonly AppSettings _appSettings;
        private readonly Web3 _web3;
        private readonly Dictionary<string, string> _tokenAbis;

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

            // 2. Check and approve allowance if needed
            var sellAmountWei = new BigInteger(decimal.Parse(quote.SellAmount));
            
            // Check if Permit2 is present in the quote and requires signing
            string? permit2Signature = null;
            if (quote.Permit2?.Eip712 != null)
            {
                Console.WriteLine("Quote contains Permit2 data, generating signature...");
                permit2Signature = GeneratePermit2Signature(quote.Permit2.Eip712);
                Console.WriteLine($"Permit2 signature generated: {permit2Signature}");
            }

            // if (quote.Issues?.Allowance is not null && permit2Signature == null)
            if (quote.Issues?.Allowance is not null)
            {
                // Only do regular approval if Permit2 isn't being used
                await ApproveTokenSpendingAsync(sellTokenAddress, quote.Issues.Allowance.Spender, sellAmountWei);
            }

            // 3. Execute the swap transaction
            string transactionHash;
            if (permit2Signature != null)
            {
                // Execute swap with Permit2 signature
                transactionHash = await ExecuteSwapWithPermit2Signature(quote, permit2Signature);
            }
            else
            {
                // Execute standard swap without Permit2
                transactionHash = await ExecuteStandardSwap(quote);
            }

            Console.WriteLine($"Transaction successful! Hash: {transactionHash}");
            return transactionHash;
        }

        private async Task<string> ExecuteStandardSwap(ZeroExQuoteResponse quote)
        {
            Console.WriteLine("Executing standard swap...");
            
            var txInput = new TransactionInput
            {
                From = _web3.TransactionManager.Account.Address,
                To = quote.Transaction.To,
                Data = quote.Transaction.Data,
                Value = new HexBigInteger(new BigInteger(decimal.Parse(quote.Transaction.Value ?? "0"))),
                Gas = new HexBigInteger(new BigInteger(decimal.Parse(quote.Transaction.Gas)) * 12 / 10), // Adding 20% buffer
                GasPrice = new HexBigInteger(new BigInteger(decimal.Parse(quote.Transaction.GasPrice)))
            };

            Console.WriteLine("Sending transaction...");
            var transactionHash = await _web3.Eth.TransactionManager.SendTransactionAsync(txInput);

            Console.WriteLine($"Transaction sent: {transactionHash}");
            await WaitForTransactionReceipt(transactionHash);
            
            return transactionHash;
        }

        private async Task<string> ExecuteSwapWithPermit2Signature(ZeroExQuoteResponse quote, string permit2Signature)
        {
            Console.WriteLine("Executing swap with Permit2 signature...");
            
            // Get the original transaction data
            string transactionData = quote.Transaction.Data;
            
            // Handle signature prefixes consistently
            string dataHex = transactionData.StartsWith("0x") ? transactionData.Substring(2) : transactionData;
            string signatureHex = permit2Signature.StartsWith("0x") ? permit2Signature.Substring(2) : permit2Signature;
            
            // Calculate signature length in bytes
            int signatureLengthInBytes = signatureHex.Length / 2;
            
            // Create a BigInteger for the length and convert to hex
            // This matches ethers.js hexZeroPad function behavior
            var signatureLengthBigInt = new BigInteger(signatureLengthInBytes);
            
            // Convert to a hex string WITHOUT "0x" prefix, padded to 64 characters (32 bytes)
            string signatureLengthHex = signatureLengthBigInt.ToString("x").PadLeft(64, '0');
            
            // Concatenate: original tx data + signature length (32 bytes) + signature
            string fullTransactionData = "0x" + dataHex + signatureLengthHex + signatureHex;
            
            Console.WriteLine($"Transaction data with appended signature: {fullTransactionData}");
            
            // Create the transaction input
            var txInput = new TransactionInput
            {
                From = _web3.TransactionManager.Account.Address,
                To = quote.Transaction.To,
                Data = fullTransactionData,
                Value = new HexBigInteger(new BigInteger(decimal.Parse(quote.Transaction.Value ?? "0"))),
                Gas = new HexBigInteger(new BigInteger(decimal.Parse(quote.Transaction.Gas)) * 12 / 10), // Adding 20% buffer
                GasPrice = new HexBigInteger(new BigInteger(decimal.Parse(quote.Transaction.GasPrice)))
            };
            
            Console.WriteLine("Sending transaction...");
            
            try {
                // Send the transaction
                var transactionHash = await _web3.Eth.TransactionManager.SendTransactionAsync(txInput);
                Console.WriteLine($"Transaction sent: {transactionHash}");
                
                await WaitForTransactionReceipt(transactionHash);
                
                return transactionHash;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending transaction: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        private async Task WaitForTransactionReceipt(string transactionHash)
        {
            // Wait for the transaction to be mined
            Console.WriteLine("Waiting for transaction to be mined...");
            var receipt = await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            
            int attempts = 0;
            while (receipt == null && attempts < 30) // Limit to 30 attempts (2.5 minutes)
            {
                await Task.Delay(5000); // Check every 5 seconds
                receipt = await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
                attempts++;
                Console.WriteLine($"Waiting for receipt... Attempt {attempts}/30");
            }
            
            if (receipt == null)
            {
                Console.WriteLine("Transaction is taking longer than expected to be mined.");
                Console.WriteLine($"You can check the status manually: https://polygonscan.com/tx/{transactionHash}");
                throw new Exception("Transaction is taking too long to be mined.");
            }
            
            if (receipt.Status.Value != 1)
            {
                Console.WriteLine($"Transaction failed with status: {receipt.Status.Value}");
                Console.WriteLine($"Gas used: {receipt.GasUsed.Value}");
                throw new Exception("Transaction failed on-chain");
            }
            
            Console.WriteLine($"Transaction successful! Gas used: {receipt.GasUsed.Value}");
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
                await WaitForTransactionReceipt(approveTxHash);
                
                Console.WriteLine("Token approval successful!");
            }
            else
            {
                Console.WriteLine("Token allowance is sufficient, no approval needed");
            }
        }

        private string GeneratePermit2Signature(EIP712? eip712Data)
        {
            Console.WriteLine("Generating Permit2 signature...");

            if (eip712Data == null)
            {
                throw new ArgumentNullException(nameof(eip712Data), "EIP712 data cannot be null");
            }

            // Create a JSON representation of the typed data with EXACT case matching
            var typedDataJson = JsonConvert.SerializeObject(new
            {
                types = new
                {
                    // IMPORTANT: Use PascalCase for type names to match the primaryType
                    PermitTransferFrom = eip712Data.Types.PermitTransferFrom?.Select(t => new { name = t.Name, type = t.Type }),
                    TokenPermissions = eip712Data.Types.TokenPermissions?.Select(t => new { name = t.Name, type = t.Type }),
                    EIP712Domain = eip712Data.Types.EIP712Domain?.Select(t => new { name = t.Name, type = t.Type })
                },
                domain = new
                {
                    name = eip712Data.Domain.Name,
                    version = "1",
                    chainId = eip712Data.Domain.ChainId,
                    verifyingContract = eip712Data.Domain.VerifyingContract
                },
                primaryType = eip712Data.PrimaryType,
                message = new
                {
                    permitted = new
                    {
                        token = eip712Data.Message.Permitted.Token,
                        amount = eip712Data.Message.Permitted.Amount
                    },
                    spender = eip712Data.Message.Spender,
                    nonce = eip712Data.Message.Nonce,
                    deadline = eip712Data.Message.Deadline
                }
            }, new JsonSerializerSettings
            {
                // Custom contract resolver that preserves dictionary key case
                ContractResolver = new DefaultContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            });

            Console.WriteLine($"Typed data JSON: {typedDataJson}");

            // Create an EthECKey from the private key
            var privateKey = _appSettings.PrivateKey;
            var ethECKey = new EthECKey(privateKey);

            // Use the Eip712TypedDataSigner with the JSON directly
            var typedDataSigner = new Eip712TypedDataSigner();
            var signatureString = typedDataSigner.SignTypedDataV4(typedDataJson, ethECKey);
            
            Console.WriteLine($"Generated signature: {signatureString}");
            
            // Also compute and log the hash for debugging purposes
            var encodedData = Eip712TypedDataEncoder.Current.EncodeTypedData(typedDataJson);
            var hash = Sha3Keccack.Current.CalculateHash(encodedData);
            Console.WriteLine($"EIP-712 hash: 0x{hash.ToHex()}");
            
            return signatureString;
        }
    }
}