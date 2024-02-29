using Cloud5mins.ShortenerTools.Core.Domain;
using Cloud5mins.ShortenerTools.Core.Domain.Models;
using Cloud5mins.ShortenerTools.Functions.Configurations;
using Cloud5mins.ShortenerTools.Functions.Extensions;
using Cloud5mins.ShortenerTools.Functions.Utils;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static Cloud5mins.ShortenerTools.Functions.Utils.Constants;

namespace Cloud5mins.ShortenerTools
{
    public class Program
    {
        public static async Task Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults(worker =>
                {
                    worker.UseNewtonsoftJson();
                }, options =>
                {
                    options.EnableUserCodeException = true; // By default, exceptions thrown by your code can end up wrapped in an RpcException.
                                                            // Set the EnableUserCodeException property to "true" as part of configuring the builder.
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddApplicationInsightsTelemetryWorkerService();
                    services.ConfigureFunctionsApplicationInsights();

                    services.AddCustomOpenApiService();

                    services.AddOptions<ShortenerSettings>()
                    .Configure<IConfiguration>((settings, configuration) =>
                    {
                        configuration.GetSection(ConfigKeys.ShortenerSettings).Bind(settings);
                    });

                    services.AddOptions<AzureAdOptions>()
                    .Configure<IConfiguration>((settings, configuration) =>
                    {
                        configuration.GetSection(ConfigKeys.AzureAdOptions).Bind(settings);
                    });

                    services.AddAzureClients(clientBuilder =>
                    {
                        clientBuilder.AddTableServiceClient(context.Configuration.GetSection(ConfigKeys.AzureWebJobsStorage));
                    });

                    services.AddScoped<AzureADJwtBearerValidation>();
                    services.AddSingleton<StorageTableHelper>();
                })
                .ConfigureOpenApi()
                .Build();

            await host.RunAsync();
        }
    }
}