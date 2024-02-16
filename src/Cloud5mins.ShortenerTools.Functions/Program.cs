using Cloud5mins.ShortenerTools.Core.Models;
using Cloud5mins.ShortenerTools.Functions.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cloud5mins.ShortenerTools
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var hostBuilder = Host.CreateDefaultBuilder(args)
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices((hostContext, services) =>
                {
                    var configuration = hostContext.Configuration;

                    services.AddSingleton(imp => configuration.GetSection(Constants.ConfigKeys.ShortenerSettings).Get<ShortenerSettings>());
                }); ;

            return hostBuilder;
        }
    }
}
