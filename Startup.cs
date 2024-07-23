using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: FunctionsStartup(typeof(sampleapp.function.Startup))]

namespace sampleapp.function
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddLogging(builder => {
                builder.AddConsole();
                builder.AddApplicationInsights(configureTelemetryConfiguration: (config) =>
                    config.ConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")!,
                    configureApplicationInsightsLoggerOptions: (options) => { });
            });
        }
    }
}
