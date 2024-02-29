using System.Net;
using System.Net.Mime;
using Cloud5mins.ShortenerTools.Core.Domain;
using Cloud5mins.ShortenerTools.Core.Domain.Models;
using Cloud5mins.ShortenerTools.Core.Messages;
using Cloud5mins.ShortenerTools.Functions.Utils;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using static Cloud5mins.ShortenerTools.Functions.Utils.Constants.Authorizations;

namespace Cloud5mins.ShortenerTools.Functions.UrlFunctions
{
    internal class UrlList(ILoggerFactory loggerFactory,
        AzureADJwtBearerValidation azureADJwtBearerValidation,
        StorageTableHelper storageTableHelper,
        IOptions<ShortenerSettings> options)
    {
        private readonly ILogger _logger = loggerFactory.CreateLogger<UrlList>();
        private readonly ShortenerSettings _settings = options.Value;

        [Function("UrlList")]
        [OpenApiOperation(operationId: nameof(UrlList), tags: [Constants.OpenApi.FunctionTag], Summary = "List of Urls", Description = "This returns list of Urls.", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiSecurity(Schemes.Bearer, SecuritySchemeType.Http, BearerFormat = Schemes.BearerFormat, In = OpenApiSecurityLocationType.Header, Name = Headers.Authorization, Scheme = OpenApiSecuritySchemeType.Bearer)]
        [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(ListResponse), Description = "Successful operation")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "UrlList")] HttpRequestData req)
        {
            _logger.LogInformation($"Starting UrlList...");

            var isValidated = await azureADJwtBearerValidation.ValidateTokenAsync(req.Headers);
            if (!isValidated)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                return unauthorizedResponse;
            }

            var result = new ListResponse();

            try
            {
                result.UrlList = await storageTableHelper.GetAllShortUrlsAsync();
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