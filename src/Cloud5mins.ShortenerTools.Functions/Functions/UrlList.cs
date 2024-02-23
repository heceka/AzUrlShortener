/*
```c#
Input:


Output:
    {
        "Url": "https://SOME_URL",
        "Clicks": 0,
        "PartitionKey": "d",
        "title": "Quickstart: Create your first function in Azure using Visual Studio"
        "RowKey": "doc",
        "Timestamp": "0001-01-01T00:00:00+00:00",
        "ETag": "W/\"datetime'2020-05-06T14%3A33%3A51.2639969Z'\""
    }
*/

using System.Net;
using System.Security.Claims;
using Cloud5mins.ShortenerTools.Core.Domain;
using Cloud5mins.ShortenerTools.Core.Domain.Models;
using Cloud5mins.ShortenerTools.Core.Messages;
using Cloud5mins.ShortenerTools.Functions.Utils;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace Cloud5mins.ShortenerTools.Functions
{
    public class UrlList
    {
        private readonly ILogger _logger;
        private readonly ShortenerSettings _settings;

        public UrlList(ILoggerFactory loggerFactory, ShortenerSettings settings)
        {
            _logger = loggerFactory.CreateLogger<UrlList>();
            _settings = settings;
        }

        [Function("UrlList")]
        [OpenApiOperation(operationId: "UrlList", tags: ["UrlList"], Summary = "UrlList", Description = "This returns list of Urls.", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiSecurity(Constants.Authorizations.OpenApiSecurity.FunctionKey, SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Header)]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "UrlList")] HttpRequestData req,
            ClaimsPrincipal principal)
        {
            _logger.LogInformation($"Starting UrlList...");

            // This API endpoint requires the "Greeting.Read" scope to be present, if it is
            // not, then reject the request with a 403.
            if (!principal.Claims.Any(c => c.Type == "http://schemas.microsoft.com/identity/claims/scope" && c.Value.Split(' ').Contains(".default")))
            {
                var forbiddenResponse = req.CreateResponse(HttpStatusCode.Forbidden);
                return forbiddenResponse;
            }

            var result = new ListResponse();
            string userId = string.Empty;

            StorageTableHelper stgHelper = new StorageTableHelper(_settings.DataStorage);

            try
            {
                result.UrlList = await stgHelper.GetAllShortUrlsAsync();
                result.UrlList = result.UrlList.Where(p => !(p.IsArchived ?? false)).ToList();
                var host = string.IsNullOrEmpty(_settings.CustomDomain) ? req.Url.Host : _settings.CustomDomain;
                foreach (ShortUrlEntity url in result.UrlList)
                {
                    url.ShortUrl = Utility.GetShortUrl(host, url.RowKey);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error was encountered.");
                var badres = req.CreateResponse(HttpStatusCode.BadRequest);
                await badres.WriteAsJsonAsync(new { ex.Message });
                return badres;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }
    }
}
