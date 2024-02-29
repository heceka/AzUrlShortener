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
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using static Cloud5mins.ShortenerTools.Functions.Utils.Constants.Authorizations;

namespace Cloud5mins.ShortenerTools.Functions.UrlFunctions
{
    internal class UrlCreate(ILoggerFactory loggerFactory,
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

        [Function("UrlCreate")]
        [OpenApiOperation(operationId: nameof(UrlCreate), tags: [Constants.OpenApi.FunctionTag], Summary = "Create URL", Description = "This creates URL.", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiSecurity(Schemes.Bearer, SecuritySchemeType.Http, BearerFormat = Schemes.BearerFormat, In = OpenApiSecurityLocationType.Header, Name = Headers.Authorization, Scheme = OpenApiSecuritySchemeType.Bearer)]
        [OpenApiRequestBody(MediaTypeNames.Application.Json, typeof(ShortRequest), Description = "URL create model", Required = true)]
        [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(ShortRequest), Description = "Successful operation")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "UrlCreate")] HttpRequestData req)
        {
            _logger.LogInformation($"__trace creating shortURL: {req}");

            var isValidated = await azureADJwtBearerValidation.ValidateTokenAsync(req.Headers);
            if (!isValidated)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                return unauthorizedResponse;
            }

            ShortRequest input;
            ShortResponse result;

            try
            {
                if (req == null)
                    return req.CreateResponse(HttpStatusCode.NotFound);

                using (var reader = new StreamReader(req.Body))
                {
                    var strBody = await reader.ReadToEndAsync();
                    input = JsonSerializer.Deserialize<ShortRequest>(strBody, s_readOptions);
                    if (input == null)
                        return req.CreateResponse(HttpStatusCode.NotFound);
                }

                // If the Url parameter only contains whitespaces or is empty return with BadRequest.
                if (string.IsNullOrWhiteSpace(input.Url))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteAsJsonAsync(new { Message = "The url parameter can not be empty." });
                    return badResponse;
                }

                // Validates if input.url is a valid aboslute url, aka is a complete refrence to the resource, ex: http(s)://google.com
                if (!Uri.IsWellFormedUriString(input.Url, UriKind.Absolute))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteAsJsonAsync(new { Message = $"{input.Url} is not a valid absolute Url. The Url parameter must start with 'http://' or 'http://'." });
                    return badResponse;
                }

                string longUrl = input.Url.Trim();
                string vanity = string.IsNullOrWhiteSpace(input.Vanity) ? "" : input.Vanity.Trim();
                string title = string.IsNullOrWhiteSpace(input.Title) ? "" : input.Title.Trim();

                ShortUrlEntity newRow;

                if (!string.IsNullOrEmpty(vanity))
                {
                    newRow = new ShortUrlEntity(longUrl, vanity, title, input.Schedules);
                    if (await storageTableHelper.IfShortUrlEntityExistAsync(newRow))
                    {
                        var badResponse = req.CreateResponse(HttpStatusCode.Conflict);
                        await badResponse.WriteAsJsonAsync(new { Message = "This Short URL already exist." });
                        return badResponse;
                    }
                }
                else
                    newRow = new ShortUrlEntity(longUrl, await Utility.GetValidEndUrl(vanity, storageTableHelper), title, input.Schedules);

                await storageTableHelper.SaveShortUrlEntityAsync(newRow);

                var host = string.IsNullOrEmpty(_settings.CustomDomain) ? req.Url.Host : _settings.CustomDomain.ToString();
                result = new ShortResponse(host, newRow.Url, newRow.RowKey, newRow.Title);

                _logger.LogInformation("Short Url created.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error was encountered.");

                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { ex.Message });
                return badResponse;
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
        // [Required] The url you wish to have a short version for
        "url": "https://docs.microsoft.com/en-ca/azure/azure-functions/functions-create-your-first-function-visual-studio",
        
        // [Optional] Title of the page, or text description of your choice.
        "title": "Quickstart: Create your first function in Azure using Visual Studio"

        // [Optional] the end of the URL. If nothing one will be generated for you.
        "vanity": "azFunc"
    }

Output:
    {
        "ShortUrl": "http://c5m.ca/azFunc",
        "LongUrl": "https://docs.microsoft.com/en-ca/azure/azure-functions/functions-create-your-first-function-visual-studio"
    }
*/