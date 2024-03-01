using System.Net;
using Cloud5mins.ShortenerTools.Core.Domain;
using Cloud5mins.ShortenerTools.Core.Domain.Models;
using Cloud5mins.ShortenerTools.Functions.Utils;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

namespace Cloud5mins.ShortenerTools.Functions.UrlFunctions
{
    public class UrlRedirect(ILoggerFactory loggerFactory,
        StorageTableHelper storageTableHelper,
        IOptions<ShortenerSettings> options)
    {
        private readonly ILogger _logger = loggerFactory.CreateLogger<UrlRedirect>();
        private readonly ShortenerSettings _settings = options.Value;

        [Function("UrlRedirect")]
        [OpenApiOperation(operationId: nameof(UrlRedirect), tags: [Constants.OpenApi.FunctionTag], Summary = "Redirect URL", Description = "This redirects URL.", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiParameter(nameof(shortUrl), In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithoutBody(HttpStatusCode.Redirect)]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Redirect/{shortUrl}")] HttpRequestData req,
            string shortUrl)
        {
            string redirectUrl = "https://www.opet.com.tr";

            if (!string.IsNullOrWhiteSpace(shortUrl))
            {
                redirectUrl = _settings.DefaultRedirectUrl ?? redirectUrl;

                var tempUrl = new ShortUrlEntity(string.Empty, shortUrl);
                var newUrl = await storageTableHelper.GetShortUrlEntityAsync(tempUrl);

                if (newUrl != null)
                {
                    _logger.LogInformation($"Found it: {newUrl.Url}");
                    newUrl.Clicks++;
                    await storageTableHelper.SaveClickStatsEntityAsync(new ClickStatsEntity(newUrl.RowKey));
                    await storageTableHelper.SaveShortUrlEntityAsync(newUrl);
                    redirectUrl = WebUtility.UrlDecode(newUrl.ActiveUrl);
                }
            }
            else
            {
                _logger.LogInformation("Bad Link, resorting to fallback.");
            }

            var res = req.CreateResponse(HttpStatusCode.Redirect);
            res.Headers.Add("Location", redirectUrl);
            return res;
        }
    }
}
