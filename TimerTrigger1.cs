using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;
using Azure.Storage.Blobs;

namespace sampleapp.function
{
    public class TimerTrigger1
    {
        private readonly ILogger<TimerTrigger1> _logger;

        public TimerTrigger1(ILogger<TimerTrigger1> logger)
        {
            _logger = logger;
        }

        [Function("TimerTrigger1")]
        public async Task Run([TimerTrigger("0 0 22 * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation("Testing blob access");
            
            try {
                string connectionString = await GetConnectionString();
                string containerName = "dtl-backup";

                BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                await foreach (var blobItem in containerClient.GetBlobsAsync())
                {
                    _logger.LogInformation($"Blob name: {blobItem.Name}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred: {ex.Message}", ex);
            }

            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            
            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }

        private async Task<string> GetConnectionString() {
            var kvUri = new Uri(Environment.GetEnvironmentVariable("KeyVaultUri")!);
            var secretClient = new SecretClient(vaultUri: kvUri, credential: new DefaultAzureCredential());
            KeyVaultSecret secret = await secretClient.GetSecretAsync("StorageAccount1ConnectionString");
            return secret.Value;
        }
    }
}
