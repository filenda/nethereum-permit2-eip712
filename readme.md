# BRLA to USDC Token Swap Application

This is a simple .NET 8 console application that uses Nethereum and the 0x.org SWAP API 2.0 to convert BRLA tokens to USDC on the Polygon network.

## Features

- Swaps BRLA tokens to USDC using 0x.org's API
- Configurable via appsettings.json
- Handles token approvals automatically
- Runs on the Polygon network
- Built with .NET 8 and Nethereum

## Project Structure:

- Program.cs - Main entry point
- Models/ - API response models
- Services/ - Services for API and swap interactions
- Configuration/ - Configuration classes
- appsettings.json - Configuration file

## Prerequisites

- .NET 8 SDK
- A wallet with BRLA tokens on Polygon
- Enough MATIC for gas fees

## Setup

1. Clone or download the project
2. Update the `appsettings.json` file with your configuration:
   - `PrivateKey`: Your wallet's private key (keep this secure!)
   - `BrlaTokenAddress`: BRLA token contract address on Polygon
   - `UsdcTokenAddress`: USDC token contract address on Polygon
   - `ZeroExApiKey`: (Optional) Your 0x API key if you have one

3. Install the required NuGet packages:

```bash
dotnet add package Nethereum.Web3
dotnet add package Microsoft.Extensions.Configuration
dotnet add package Microsoft.Extensions.Configuration.Json
dotnet add package Microsoft.Extensions.DependencyInjection
dotnet add package Microsoft.Extensions.Http
dotnet add package Newtonsoft.Json
```

## Usage

Run the application with:

```bash
dotnet run
```

Or specify the amount directly:

```bash
dotnet run 10.5
```

The application will:
1. Check your token allowance and approve if needed
2. Get a quote from 0x.org API
3. Execute the swap transaction
4. Display the transaction hash and a link to view it on PolygonScan

## Security Considerations

- **NEVER commit your private key to version control**
- In a production environment, consider using a secure secret manager instead of storing the private key in appsettings.json
- Add proper error handling and logging for production use

## Configuration Reference

In the `appsettings.json` file:

- `ZeroExApiBaseUrl`: Base URL for 0x API (default: "https://polygon.api.0x.org/")
- `PolygonRpcUrl`: Polygon RPC URL (default: "https://polygon-rpc.com")
- `PrivateKey`: Your wallet's private key
- `BrlaTokenAddress`: BRLA token contract address on Polygon
- `UsdcTokenAddress`: USDC token contract address on Polygon
- `ZeroExApiKey`: Your 0x API key (optional)
- `ChainId`: Chain ID for Polygon (137)

## How It Works

1. The application gets a quote from the 0x API for the BRLA to USDC swap
2. It checks if the wallet has approved the 0x contract to spend BRLA tokens
3. If needed, it sends an approval transaction
4. It then executes the swap transaction using the data from the 0x API
5. The application waits for the transaction to be mined and verifies success

## Notes

- The application assumes BRLA has 18 decimals like most ERC20 tokens
- The slippage tolerance is set to 1% by default
- Gas estimates include a 20% buffer to ensure transactions don't fail