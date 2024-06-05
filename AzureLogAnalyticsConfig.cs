namespace AzureLogAnalyticsLibrary
{
    public class AzureLogAnalyticsConfig
    {
        public string KeyVaultUrl { get; set; }
        public string CertificateName { get; set; }
        public string SubscriptionId { get; set; }
        public string ResourceGroupName { get; set; }
        public string WorkspaceName { get; set; }
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public int LogQueryDays { get; set; }
    }
}
