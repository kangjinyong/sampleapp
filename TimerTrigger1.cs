using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

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
            try {
                BlobServiceClient blobServiceClient1 = new BlobServiceClient(new Uri(await GetUri("1")), GetAzureCredential());
                BlobServiceClient blobServiceClient2 = new BlobServiceClient(new Uri(await GetUri("2")), GetAzureCredential());
                BlobContainerClient containerClient1 = blobServiceClient1.GetBlobContainerClient(Environment.GetEnvironmentVariable("ContainerNameFrom")!);
                BlobContainerClient containerClient2 = blobServiceClient2.GetBlobContainerClient(Environment.GetEnvironmentVariable("ContainerNameTo")!);
                await containerClient2.CreateIfNotExistsAsync();
                await CopyFiles(containerClient1, containerClient2);
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

        private DefaultAzureCredential GetAzureCredential() {
            string userAssignedClientId = Environment.GetEnvironmentVariable("UserAssignedClientId")!;
            var credentialOptions = new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = userAssignedClientId
            };
            return new DefaultAzureCredential(credentialOptions);
        }

        private async Task<string> GetUri(string storageAccount) {
            var kvUri = new Uri(Environment.GetEnvironmentVariable("KeyVaultUri")!);
            var secretClient = new SecretClient(vaultUri: kvUri, credential: GetAzureCredential());
            KeyVaultSecret secret = await secretClient.GetSecretAsync(string.Format("StorageAccount{0}Uri", storageAccount));
            return secret.Value;
        }

        private async Task CopyFiles(BlobContainerClient containerFrom, BlobContainerClient containerTo) {
            await foreach (var blobItem in containerFrom.GetBlobsAsync())
            {
                string fromName = blobItem.Name;
                string[] fromNameArray = fromName.Split("/");
                string toName = fromNameArray[fromNameArray.Length-1];
                BlobClient fromClient = containerFrom.GetBlobClient(fromName);
                _logger.LogInformation(string.Format("fromName: {0}", fromName));
                BlobClient toClient = containerTo.GetBlobClient(toName);
                _logger.LogInformation(string.Format("toName: {0}", toName));
                CopyStatus copyStatus = CopyStatus.Pending;
                await toClient.StartCopyFromUriAsync(fromClient.Uri);
                _logger.LogInformation(string.Format("Copying"));

                while (copyStatus == CopyStatus.Pending)
                {
                    await Task.Delay(100);
                    BlobProperties properties = await toClient.GetPropertiesAsync();
                    copyStatus = properties.CopyStatus;
                    _logger.LogInformation(string.Format("Copy Status: {0}", copyStatus));
                }

                if (copyStatus == CopyStatus.Success)
                {
                    _logger.LogInformation(string.Format("{0} copied successfully to {1}.", fromName, toName));
                }
                else
                {
                    _logger.LogInformation(string.Format("{0} copy failed or was canceled.", fromName));
                }
            }
        }
    }
}
