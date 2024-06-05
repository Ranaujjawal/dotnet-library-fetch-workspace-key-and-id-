using Azure.Identity;
using Azure.Core;
using System.Security.Cryptography.X509Certificates;
using Azure.Security.KeyVault.Certificates;
using Azure.ResourceManager;
using Azure.ResourceManager.OperationalInsights;
using Azure.ResourceManager.OperationalInsights.Models;
using Newtonsoft.Json.Linq;
using Azure.Monitor.Query;
using Serilog;

namespace AzureLogAnalyticsLibrary
{
    public class AzureLogAnalyticsClient
    {
        private readonly AzureLogAnalyticsConfig _config;
        public X509Certificate2 Certificate;

        public AzureLogAnalyticsClient(AzureLogAnalyticsConfig config)
        {
            _config = config;
        }

        public async Task InitializeAsync()
        {
           Certificate = await GetCertificateAsync();
        }

        private async Task<X509Certificate2> GetCertificateAsync()
        {
            var client = new CertificateClient(new Uri(_config.KeyVaultUrl), new ManagedIdentityCredential());
            try
            {
                Certificate = await client.DownloadCertificateAsync(_config.CertificateName);
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ManagedIdentityCredential failed: {ex.Message}, trying DefaultAzureCredential.");
                client = new CertificateClient(new Uri(_config.KeyVaultUrl), new DefaultAzureCredential());
                try
                {
                    Certificate = await client.DownloadCertificateAsync(_config.CertificateName);
                    //Console.WriteLine($"Certificate Thumbprint: {_certificate.Thumbprint}");
                }
                catch (Exception dex)
                {
                    Console.WriteLine($"DefaultAzureCredential failed: {dex.Message}");
                    throw;
                }
            }
            return Certificate;
        }

        public async Task<string> GetWorkspaceIdAsync()
        {
            var accessToken = await GetAccessTokenAsync();
            return await GetWorkspaceId(accessToken);
        }

        public async Task<string> GetWorkspaceKeyAsync()
        {
            var cred = new ClientCertificateCredential(_config.TenantId, _config.ClientId, Certificate);
            var client = new ArmClient(cred);
            var workspaceResourceId = OperationalInsightsWorkspaceResource.CreateResourceIdentifier(_config.SubscriptionId, _config.ResourceGroupName, _config.WorkspaceName);
            var workspaceResource = client.GetOperationalInsightsWorkspaceResource(workspaceResourceId);
            var result = await workspaceResource.GetSharedKeysAsync();
            return result.Value?.PrimarySharedKey;
        }

        private async Task<string> GetAccessTokenAsync()
        {
            var credential = new ClientCertificateCredential(_config.TenantId, _config.ClientId, Certificate);
            var accessToken = await credential.GetTokenAsync(new TokenRequestContext(new[] { "https://management.azure.com/.default" }));
            return accessToken.Token;
        }

        private async Task<string> GetWorkspaceId(string accessToken)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            var requestUrl = $"https://management.azure.com/subscriptions/{_config.SubscriptionId}/resourceGroups/{_config.ResourceGroupName}/providers/Microsoft.OperationalInsights/workspaces/{_config.WorkspaceName}?api-version=2021-12-01-preview";
            var response = await httpClient.GetAsync(requestUrl);
            var responseContent = await response.Content.ReadAsStringAsync();
            var responseObject = JObject.Parse(responseContent);
            return responseObject["properties"]["customerId"].ToString();
        }

        public static async Task FetchLogsAsync(X509Certificate2 certificate, string workspaceId, string tenantId, string clientId, string query, int logQueryDays)
        {
            var cred = new ClientCertificateCredential(tenantId, clientId, certificate);
            var logsClient = new LogsQueryClient(cred);
            var response = await logsClient.QueryWorkspaceAsync(workspaceId, query, new QueryTimeRange(TimeSpan.FromDays(logQueryDays)));
            foreach (var table in response.Value.AllTables)
            {
                foreach (var row in table.Rows)
                {
                    foreach (var column in row)
                    {
                        Console.Write($"{column} ");
                    }
                             Console.WriteLine(); 
                  }
                          Console.WriteLine(); 

                
            }
        }

        public static void WriteLogsToAzureLogAnalytics(string workspaceId, string workspaceKey, string logType, object logData)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.AzureAnalytics(workspaceId, workspaceKey, logName: logType)
                .CreateLogger();
            Log.Information("Log message with multiple properties: {@LogData}", logData);
            Log.CloseAndFlush();
        }
    }
}
