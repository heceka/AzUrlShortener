using Cloud5mins.ShortenerTools.Core.Domain.Models;
using Cloud5mins.ShortenerTools.Functions.Configurations;
using Cloud5mins.ShortenerTools.Functions.Middlewares;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
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
                .ConfigureFunctionsWorkerDefaults(builder =>
                {
                    builder.UseMiddleware<AuthenticationMiddleware>();
                    //builder.UseMiddleware<AuthorizationMiddleware>();
                }, options =>
                {
                    /*
                     * By default, exceptions thrown by your code can end up wrapped in an RpcException.
                     * To remove this extra layer, set the EnableUserCodeException property to "true" as part of configuring the builder
                    */
                    //options.EnableUserCodeException = true;
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddApplicationInsightsTelemetryWorkerService();
                    services.ConfigureFunctionsApplicationInsights();

                    services
                        .AddOptions<AzureAuthenticationOptions>()
                        .Configure<IConfiguration>((settings, configuration) =>
                        {
                            configuration.GetSection(ConfigKeys.AzureAuthenticationOptions).Bind(settings);
                        });

                    //TODO: hkilic - IOptions<T> seklinde duzenlenecek. https://learn.microsoft.com/en-us/azure/azure-functions/functions-dotnet-dependency-injection#working-with-options-and-settings
                    var shortenerSettings = context.Configuration.GetSection(ConfigKeys.ShortenerSettings).Get<ShortenerSettings>();
                    services.AddSingleton(shortenerSettings);
                })
                .ConfigureOpenApi()
                .Build();

            await host.RunAsync();
        }
    }
}