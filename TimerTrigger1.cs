using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

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
            string dateTimeStamp = DateTimeOffset.Now.ToOffset(TimeSpan.FromHours(8)).ToString("yyyyMMddHHmmssfff");
            List<string> checksums = new List<string>();
            string checksumFilename = string.Format("{0}/Checksum_{0}.csv", dateTimeStamp);
            await foreach (var blobItem in containerFrom.GetBlobsAsync())
            {
                string fromName = blobItem.Name;
                string[] fromNameArray = fromName.Split("/");
                string toName = string.Format("{0}/{1}_{0}.csv", dateTimeStamp, fromNameArray[fromNameArray.Length-1].Split(".")[0]);
                BlobClient fromClient = containerFrom.GetBlobClient(fromName);
                BlobClient toClient = containerTo.GetBlobClient(toName);
                BlobDownloadStreamingResult stream = (await fromClient.DownloadStreamingAsync()).Value;

                try {
                    using (ZipArchive archive = new ZipArchive(stream.Content, ZipArchiveMode.Read)) {
                        Stream unzippedStream = archive.Entries[0].Open();
                        await toClient.UploadAsync(unzippedStream, overwrite: true);
                        _logger.LogInformation(string.Format("{0} successfully unzipped and copied to {1}.", fromName, toName));

                        using (SHA256 sha256 = SHA256.Create())
                        {
                            byte[] hash = sha256.ComputeHash(unzippedStream);
                            StringBuilder sb = new StringBuilder();
                            foreach (byte b in hash)
                            {
                                sb.Append(b.ToString("x2"));
                            }
                            checksums.Add(string.Format("{0} ~ {1}", toName, sb.ToString()));
                        }
                    }
                }     
                catch {
                    _logger.LogInformation(string.Format("{0} cannot be unzipped and copied.", fromName));
                } 
            }
            BlobClient checksumClient = containerTo.GetBlobClient(checksumFilename);
            byte[] byteArray = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, checksums));
            using (MemoryStream stream = new MemoryStream(byteArray))
            {
                await checksumClient.UploadAsync(stream, overwrite: true);
            }
        }
    }
}
