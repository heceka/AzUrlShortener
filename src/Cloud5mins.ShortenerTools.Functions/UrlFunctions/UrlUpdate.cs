using System.Net;
using System.Net.Mime;
using System.Text.Json;
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
using static Cloud5mins.ShortenerTools.Functions.Utils.Constants.Authorizations;

namespace Cloud5mins.ShortenerTools.Functions.UrlFunctions
{
    internal class UrlUpdate(ILoggerFactory loggerFactory,
        AzureADJwtBearerValidation azureADJwtBearerValidation,
        StorageTableHelper storageTableHelper,
        IOptions<ShortenerSettings> options)
    {
        private readonly ILogger _logger = loggerFactory.CreateLogger<UrlList>();
        private readonly ShortenerSettings _settings = options.Value;
        private static readonly JsonSerializerOptions s_readOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        [Function("UrlUpdate")]
        [OpenApiOperation(operationId: nameof(UrlUpdate), tags: [Constants.OpenApi.FunctionTag], Summary = "Update URL", Description = "This updates URL.", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiSecurity(Schemes.Bearer, SecuritySchemeType.Http, BearerFormat = Schemes.BearerFormat, In = OpenApiSecurityLocationType.Header, Name = Headers.Authorization, Scheme = OpenApiSecuritySchemeType.Bearer)]
        [OpenApiRequestBody(MediaTypeNames.Application.Json, typeof(ShortUrlEntity), Description = "URL update model", Required = true)]
        [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(ShortUrlEntity), Description = "Successful operation")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "UrlUpdate")] HttpRequestData req)
        {
            _logger.LogInformation($"HTTP trigger - UrlUpdate");

            var isValidated = await azureADJwtBearerValidation.ValidateTokenAsync(req.Headers);
            if (!isValidated)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                return unauthorizedResponse;
            }

            ShortUrlEntity input;
            ShortUrlEntity result;

            try
            {
                if (req == null)
                    return req.CreateResponse(HttpStatusCode.NotFound);

                using (var reader = new StreamReader(req.Body))
                {
                    var strBody = await reader.ReadToEndAsync();
                    input = JsonSerializer.Deserialize<ShortUrlEntity>(strBody, s_readOptions);
                    if (input == null)
                        return req.CreateResponse(HttpStatusCode.NotFound);
                }

                // If the Url parameter only contains whitespaces or is empty return with BadRequest.
                if (string.IsNullOrWhiteSpace(input.Url))
                {
                    var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequest.WriteAsJsonAsync(new { Message = "The url parameter can not be empty." });
                    return badRequest;
                }

                // Validates if input.url is a valid aboslute url, aka is a complete refrence to the resource, ex: http(s)://google.com
                if (!Uri.IsWellFormedUriString(input.Url, UriKind.Absolute))
                {
                    var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequest.WriteAsJsonAsync(new { Message = $"{input.Url} is not a valid absolute Url. The Url parameter must start with 'http://' or 'http://'." });
                    return badRequest;
                }

                result = await storageTableHelper.UpdateShortUrlEntityAsync(input);
                var host = string.IsNullOrEmpty(_settings.CustomDomain) ? req.Url.Host : _settings.CustomDomain.ToString();
                result.ShortUrl = Utility.GetShortUrl(host, result.RowKey);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error was encountered.");

                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteAsJsonAsync(new { ex.Message });
                return badRequest;
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
    {
         // [Required]
        "PartitionKey": "d",

         // [Required]
        "RowKey": "doc",

        // [Optional] New Title for this URL, or text description of your choice.
        "title": "Quickstart: Create your first function in Azure using Visual Studio"

        // [Optional] New long Url where the the user will be redirect
        "Url": "https://SOME_URL"
    }


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