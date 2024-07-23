# AzureLogAnalyticsLibrary

AzureLogAnalyticsLibrary is a C# library for interacting with Azure Log Analytics. It uses Azure Key Vault for certificate management, and Azure Monitor Query to fetch logs from Log Analytics workspace.

## Features

- Fetch certificate from Azure Key Vault using Managed Identity or Default Azure Credential.
- Retrieve Log Analytics workspace ID and shared key.
- Fetch logs from Log Analytics workspace using Kusto queries.
- Write logs to Azure Log Analytics workspace.

## Requirements

- .NET 8.0 or later
- Azure Key Vault
- Azure Log Analytics Workspace
- Azure Managed Identity or Service Principal

## Installation

To install the necessary NuGet packages, run:

```sh
dotnet add package Azure.Identity
dotnet add package Azure.Security.KeyVault.Certificates
dotnet add package Azure.ResourceManager
dotnet add package Azure.ResourceManager.OperationalInsights
dotnet add package Azure.Monitor.Query
dotnet add package Serilog
dotnet add package Serilog.Sinks.AzureAnalytics
dotnet add package Newtonsoft.Json
dotnet add package UJR_Law.read.write.crosstenant_UJR --version 1.0.0
```

### Program.cs

```csharp
using System;
using System.Threading.Tasks;
using AzureLogAnalyticsLibrary;

class Program
{
    static async Task Main(string[] args)
    {
        var config = new AzureLogAnalyticsConfig
        {
            KeyVaultUrl = "https://<your-keyvault-name>.vault.azure.net/",
            CertificateName = "<your-certificate-name>",
            SubscriptionId = "<your-subscription-id>",
            ResourceGroupName = "<your-resource-group-name>",
            WorkspaceName = "<your-workspace-name>",
            TenantId = "<your-tenant-id>",
            ClientId = "<your-client-id>",
            LogQueryDays = 7 // Number of days for log query
        };

        var client = new AzureLogAnalyticsClient(config);
        await client.InitializeAsync();

        // Fetch Workspace ID
        var workspaceId = await client.GetWorkspaceIdAsync();
        Console.WriteLine($"Workspace ID: {workspaceId}");

        // Fetch Workspace Key
        var workspaceKey = await client.GetWorkspaceKeyAsync();
        Console.WriteLine($"Workspace Key: {workspaceKey}");

        // Fetch Logs
        string query = @"demotable_CL
                         | sort by TimeGenerated desc
                         | limit 10";;
        await AzureLogAnalyticsClient.FetchLogsAsync(client.Certificate, workspaceId, config.TenantId, config.ClientId, query, config.LogQueryDays);

        // Write Logs
        var logData = new { Message = "Test log message", Severity = "Information" };
        AzureLogAnalyticsClient.WriteLogsToAzureLogAnalytics(workspaceId, workspaceKey, "CustomLogType", logData);
    }
}
```
