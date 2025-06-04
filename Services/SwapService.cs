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
using Nethereum.Signer; // Add this for EIP712 signing
using Nethereum.Signer.EIP712; // Add this for EIP712 domain
using Nethereum.Util;
using BrlaUsdcSwap.Models;
using Nethereum.ABI.EIP712;
using Nethereum.Hex.HexConvertors.Extensions; // Add this for hex utilities

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

            // Check if Permit2 is present in the quote and requires signing
            string? permit2Signature = null;
            if (quote.Permit2?.Eip712 != null)
            {
                Console.WriteLine("Quote contains Permit2 data, generating signature...");
                permit2Signature = GeneratePermit2Signature(quote.Permit2.Eip712);
                Console.WriteLine("Permit2 signature generated successfully");
            }

            if (quote.Issues?.Allowance is not null)
            {
                // Only do regular approval if Permit2 isn't being used
                await ApproveTokenSpendingAsync(sellTokenAddress, quote.Issues.Allowance.Spender, sellAmountWei);
            }

            // 3. Execute the swap transaction
            var txInput = new TransactionInput
            {
                From = _web3.TransactionManager.Account.Address,
                To = quote.Transaction.To,
                Data = quote.Transaction.Data,
                Value = new HexBigInteger(new BigInteger(decimal.Parse(quote.Transaction.Value ?? "0"))),
                Gas = new HexBigInteger(new BigInteger(decimal.Parse(quote.Transaction.Gas)) * 12 / 10), // Adding 20% buffer to gas estimate
                GasPrice = new HexBigInteger(new BigInteger(decimal.Parse(quote.Transaction.GasPrice)))
            };

            // If we have a Permit2 signature, we need to modify the transaction data to include it
            if (permit2Signature != null)
            {
                Console.WriteLine("Appending Permit2 signature to transaction data...");

                // Remove 0x prefix if present from both data and signature
                string dataHex = txInput.Data.StartsWith("0x") ? txInput.Data.Substring(2) : txInput.Data;
                string signatureHex = permit2Signature.StartsWith("0x") ? permit2Signature.Substring(2) : permit2Signature;

                // Calculate the signature length in bytes (each hex character is half a byte)
                int signatureLengthInBytes = signatureHex.Length / 2;

                // Convert the length to a 32-byte hex string (padded)
                string signatureLengthHex = new BigInteger(signatureLengthInBytes).ToString("x").PadLeft(64, '0');

                // Concatenate: original tx data + signature length (32 bytes) + signature
                txInput.Data = "0x" + dataHex + signatureLengthHex + signatureHex;

                Console.WriteLine($"Modified transaction data with signature: {txInput.Data}");
            }

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

        private string GeneratePermit2Signature(EIP712? eip712Data)
        {
            Console.WriteLine("Generating Permit2 signature...");

            if (eip712Data == null)
            {
                throw new ArgumentNullException(nameof(eip712Data), "EIP712 data cannot be null");
            }

            // Extract the key components from the EIP712 data
            var domain = eip712Data.Domain;
            var message = eip712Data.Message;
            var types = eip712Data.Types;
            var primaryType = eip712Data.PrimaryType;

            Console.WriteLine($"Domain: {JsonConvert.SerializeObject(domain)}");
            Console.WriteLine($"Message: {JsonConvert.SerializeObject(message)}");
            Console.WriteLine($"Types: {JsonConvert.SerializeObject(types)}");
            Console.WriteLine($"Primary Type: {primaryType}");

            try
            {
                // Create domain separator
                var domainSeparator = new EIP712Domain
                {
                    Name = domain?.Name,
                    ChainId = domain?.ChainId ?? _appSettings.ChainId,
                    VerifyingContract = domain?.VerifyingContract
                };

                // Create the EIP712 typed data with the generic type parameter
                var typedData = new TypedData<EIP712Domain>
                {
                    Domain = domainSeparator,
                    PrimaryType = primaryType,
                    Types = BuildEIP712Types(types),
                    Message = BuildEIP712Message(message)
                };

                // Print TypedData for debugging
                Console.WriteLine($"TypedData: {JsonConvert.SerializeObject(typedData)}");

                // Use Eip712TypedDataSigner
                var typedDataSigner = new Eip712TypedDataSigner();
                var privateKey = _appSettings.PrivateKey;

                // Sign using the appropriate method
                var ethECKey = new EthECKey(privateKey);
                var signature = typedDataSigner.SignTypedDataV4(typedData, ethECKey);

                Console.WriteLine($"Raw signature: {signature}");

                // Parse the signature to get r, s, v
                var signatureBytes = signature.HexToByteArray();
                if (signatureBytes.Length != 65)
                {
                    throw new Exception($"Expected 65 bytes signature, got {signatureBytes.Length}");
                }

                // Extract r, s, v from the signature
                byte[] r = new byte[32];
                byte[] s = new byte[32];
                Array.Copy(signatureBytes, 0, r, 0, 32);
                Array.Copy(signatureBytes, 32, s, 0, 32);
                byte v = signatureBytes[64];

                // Handle v value according to Permit2 requirements
                // Permit2 requires v to be 27 or 28 (not 0 or 1)
                if (v < 27)
                {
                    v += 27;
                }

                // Repack the signature in the format expected by Permit2 (r, s, v)
                byte[] formattedSignature = new byte[65];
                Array.Copy(r, 0, formattedSignature, 0, 32);
                Array.Copy(s, 0, formattedSignature, 32, 32);
                formattedSignature[64] = v;

                // Convert to hex string with 0x prefix
                var formattedSignatureHex = "0x" + formattedSignature.ToHex();
                Console.WriteLine($"Formatted signature: {formattedSignatureHex}");

                return formattedSignatureHex;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating signature: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        // Helper methods to construct the EIP712 types and message
        private Dictionary<string, MemberDescription[]> BuildEIP712Types(EIP712Types? types)
        {
            var result = new Dictionary<string, MemberDescription[]>();

            // Add the domain type
            if (types?.EIP712Domain != null)
            {
                result.Add("EIP712Domain", types.EIP712Domain.Select(t =>
                    new MemberDescription { Name = t.Name, Type = t.Type }).ToArray());
            }

            // Add the primary type (PermitTransferFrom)
            if (types?.PermitTransferFrom != null)
            {
                result.Add("PermitTransferFrom", types.PermitTransferFrom.Select(t =>
                    new MemberDescription { Name = t.Name, Type = t.Type }).ToArray());
            }

            // Add TokenPermissions type
            if (types?.TokenPermissions != null)
            {
                result.Add("TokenPermissions", types.TokenPermissions.Select(t =>
                    new MemberDescription { Name = t.Name, Type = t.Type }).ToArray());
            }

            Console.WriteLine($"Built types: {JsonConvert.SerializeObject(result)}");
            return result;
        }

        private MemberValue[] BuildEIP712Message(EIP712Message? message)
        {
            var result = new List<MemberValue>();

            // Add the permitted token permissions
            if (message?.Permitted != null)
            {
                // Create a nested MemberValue for the permitted token
                var permittedValues = new List<MemberValue>
                {
                    new MemberValue { TypeName = "address", Value = message.Permitted.Token ?? string.Empty },
                    new MemberValue { TypeName = "uint256", Value = message.Permitted.Amount ?? string.Empty }
                };

                result.Add(new MemberValue
                {
                    TypeName = "TokenPermissions",
                    Value = permittedValues.ToArray()
                });
            }

            // Add other message fields
            if (message?.Spender != null)
                result.Add(new MemberValue { TypeName = "address", Value = message.Spender });

            if (message?.Nonce != null)
                result.Add(new MemberValue { TypeName = "uint256", Value = message.Nonce });

            if (message?.Deadline != null)
                result.Add(new MemberValue { TypeName = "uint256", Value = message.Deadline });

            return result.ToArray();
        }
    }
}