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
            _logger.LogInformation("Testing blob access");

            try {
                BlobServiceClient blobServiceClient1 = new BlobServiceClient(await GetConnectionString("1"));
                BlobServiceClient blobServiceClient2 = new BlobServiceClient(await GetConnectionString("2"));
                BlobContainerClient containerClient1 = blobServiceClient1.GetBlobContainerClient(Environment.GetEnvironmentVariable("ContainerNameFrom")!);
                BlobContainerClient containerClient2 = blobServiceClient1.GetBlobContainerClient(Environment.GetEnvironmentVariable("ContainerNameTo")!);
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

        private async Task<string> GetConnectionString(string storageAccount) {
            var kvUri = new Uri(Environment.GetEnvironmentVariable("KeyVaultUri")!);
            string userAssignedClientId = Environment.GetEnvironmentVariable("UserAssignedClientId")!;
            var credentialOptions = new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = userAssignedClientId
            };
            var secretClient = new SecretClient(vaultUri: kvUri, credential: new DefaultAzureCredential(credentialOptions));
            KeyVaultSecret secret = await secretClient.GetSecretAsync(string.Format("StorageAccount{0}ConnectionString", storageAccount));
            return secret.Value;
        }

        private async Task CopyFiles(BlobContainerClient containerFrom, BlobContainerClient containerTo) {
            await foreach (var blobItem in containerFrom.GetBlobsAsync())
            {
                string fromName = blobItem.Name;
                string[] fromNameArray = fromName.Split("/");
                string toName = fromNameArray[fromNameArray.Length-1];
                BlobClient fromClient = containerFrom.GetBlobClient(blobItem.Name);
                BlobClient toClient = containerTo.GetBlobClient(toName);
                CopyStatus copyStatus = CopyStatus.Pending;
                await toClient.StartCopyFromUriAsync(fromClient.Uri);

                while (copyStatus == CopyStatus.Pending)
                {
                    await Task.Delay(100);
                    BlobProperties properties = await toClient.GetPropertiesAsync();
                    copyStatus = properties.CopyStatus;
                }

                if (copyStatus == CopyStatus.Success)
                {
                    Console.WriteLine(string.Format("{0} copied successfully to {1}.", fromName, toName));
                }
                else
                {
                    Console.WriteLine(string.Format("{0} copy failed or was canceled.", fromName));
                }
            }
        }
    }
}
