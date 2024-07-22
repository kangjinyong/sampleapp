using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: FunctionsStartup(typeof(YourNamespace.Startup))]

namespace YourNamespace
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // string blobServiceClientConnectionString = Environment.GetEnvironmentVariable("BlobServiceClientConnectionString");
            // builder.Services.AddSingleton(new BlobServiceClient(blobServiceClientConnectionString));
            builder.Services.AddLogging();
        }
    }
}
