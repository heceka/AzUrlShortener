using System.Reflection;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Cloud5mins.ShortenerTools.Functions.Extensions
{
    internal static class OpenApiExtensions
    {
        internal static void AddCustomOpenApiService(this IServiceCollection services)
        {
            services.AddSingleton<IOpenApiConfigurationOptions>(_ =>
             {
                 var options = new OpenApiConfigurationOptions()
                 {
                     Info = new OpenApiInfo()
                     {
                         Version = DefaultOpenApiConfigurationOptions.GetOpenApiDocVersion(),
                         Title = $"{DefaultOpenApiConfigurationOptions.GetOpenApiDocTitle()} (Injected)",
                         Description = DefaultOpenApiConfigurationOptions.GetOpenApiDocDescription(),
                         TermsOfService = new Uri("https://github.com/Azure/azure-functions-openapi-extension"),
                         Contact = new OpenApiContact()
                         {
                             Name = "Enquiry",
                             Email = "azfunc-openapi@microsoft.com",
                             Url = new Uri("https://github.com/Azure/azure-functions-openapi-extension/issues"),
                         },
                         License = new OpenApiLicense()
                         {
                             Name = "MIT",
                             Url = new Uri("http://opensource.org/licenses/MIT"),
                         }
                     },
                     Servers = DefaultOpenApiConfigurationOptions.GetHostNames(),
                     OpenApiVersion = DefaultOpenApiConfigurationOptions.GetOpenApiVersion(),
                     IncludeRequestingHostName = DefaultOpenApiConfigurationOptions.IsFunctionsRuntimeEnvironmentDevelopment(),
                     ForceHttps = DefaultOpenApiConfigurationOptions.IsHttpsForced(),
                     ForceHttp = DefaultOpenApiConfigurationOptions.IsHttpForced(),
                 };

                 return options;
             });

            services.AddSingleton<IOpenApiHttpTriggerAuthorization>(_ =>
            {
                var auth = new OpenApiHttpTriggerAuthorization(req =>
                {
                    var result = default(OpenApiAuthorizationResult);

                    // ⬇️⬇️⬇️ Add your custom authorisation logic ⬇️⬇️⬇️
                    //
                    // CUSTOM AUTHORISATION LOGIC
                    //
                    // ⬆️⬆️⬆️ Add your custom authorisation logic ⬆️⬆️⬆️

                    return Task.FromResult(result);
                });

                return auth;
            })
            .AddSingleton<IOpenApiCustomUIOptions>(_ =>
            {
                var assembly = Assembly.GetExecutingAssembly();
                var options = new OpenApiCustomUIOptions(assembly)
                {
                    GetStylesheet = () =>
                    {
                        var result = string.Empty;

                        // ⬇️⬇️⬇️ Add your logic to get your custom stylesheet ⬇️⬇️⬇️
                        //
                        // CUSTOM LOGIC TO GET STYLESHEET
                        //
                        // ⬆️⬆️⬆️ Add your logic to get your custom stylesheet ⬆️⬆️⬆️

                        return Task.FromResult(result);
                    },
                    GetJavaScript = () =>
                    {
                        var result = string.Empty;

                        // ⬇️⬇️⬇️ Add your logic to get your custom JavaScript ⬇️⬇️⬇️
                        //
                        // CUSTOM LOGIC TO GET JAVASCRIPT
                        //
                        // ⬆️⬆️⬆️ Add your logic to get your custom JavaScript ⬆️⬆️⬆️

                        return Task.FromResult(result);
                    }
                };

                return options;
            });
        }
    }
}
