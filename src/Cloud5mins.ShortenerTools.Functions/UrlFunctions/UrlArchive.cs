using System.Net;
using System.Net.Mime;
using System.Text.Json;
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
using static Cloud5mins.ShortenerTools.Functions.Utils.Constants.Authorizations;

namespace Cloud5mins.ShortenerTools.Functions.UrlFunctions
{
    internal class UrlArchive(ILoggerFactory loggerFactory,
        AzureADJwtBearerValidation azureADJwtBearerValidation,
        StorageTableHelper storageTableHelper)
    {
        private readonly ILogger _logger = loggerFactory.CreateLogger<UrlList>();
        private static readonly JsonSerializerOptions s_readOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        [Function("UrlArchive")]
        [OpenApiOperation(operationId: nameof(UrlArchive), tags: [nameof(UrlArchive)], Summary = "Archive URL", Description = "This archives URL.", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiSecurity(Schemes.Bearer, SecuritySchemeType.Http, BearerFormat = Schemes.BearerFormat, In = OpenApiSecurityLocationType.Header, Name = Headers.Authorization, Scheme = OpenApiSecuritySchemeType.Bearer)]
        [OpenApiRequestBody(MediaTypeNames.Application.Json, typeof(ShortUrlEntity), Description = "URL click stats by day model", Required = true)]
        [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(ShortUrlEntity), Description = "Successful operation")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "UrlArchive")] HttpRequestData req)
        {
            _logger.LogInformation($"HTTP trigger - UrlArchive");

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
                    var body = await reader.ReadToEndAsync();
                    input = JsonSerializer.Deserialize<ShortUrlEntity>(body, s_readOptions);
                    if (input == null)
                        return req.CreateResponse(HttpStatusCode.NotFound);
                }

                result = await storageTableHelper.ArchiveShortUrlEntityAsync(input);
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

        // [Optional] all other properties
    }
Output:
    {
        "Url": "https://docs.microsoft.com/en-ca/azure/azure-functions/functions-create-your-first-function-visual-studio",
        "Title": "My Title",
        "ShortUrl": null,
        "Clicks": 0,
        "IsArchived": true,
        "PartitionKey": "a",
        "RowKey": "azFunc2",
        "Timestamp": "2020-07-23T06:22:33.852218-04:00",
        "ETag": "W/\"datetime'2020-07-23T10%3A24%3A51.3440526Z'\""
    }

*/