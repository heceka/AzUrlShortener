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
    internal class UrlClickStatsByDay(ILoggerFactory loggerFactory,
        AzureADJwtBearerValidation azureADJwtBearerValidation,
        StorageTableHelper storageTableHelper,
        IOptions<ShortenerSettings> options)
    {
        private readonly ILogger _logger = loggerFactory.CreateLogger<UrlClickStatsByDay>();
        private readonly ShortenerSettings _settings = options.Value;
        private static readonly JsonSerializerOptions s_readOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        [Function("UrlClickStatsByDay")]
        [OpenApiOperation(operationId: nameof(UrlClickStatsByDay), tags: [Constants.OpenApi.FunctionTag], Summary = "Get URL click stats by day", Description = "Get URL click stats by day.", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiSecurity(Schemes.Bearer, SecuritySchemeType.Http, BearerFormat = Schemes.BearerFormat, In = OpenApiSecurityLocationType.Header, Name = Headers.Authorization, Scheme = OpenApiSecuritySchemeType.Bearer)]
        [OpenApiRequestBody(MediaTypeNames.Application.Json, typeof(UrlClickStatsRequest), Description = "URL click stats by day model", Required = true)]
        [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(ClickDateList), Description = "Successful operation")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "UrlClickStatsByDay")] HttpRequestData req)
        {
            _logger.LogInformation($"HTTP trigger: UrlClickStatsByDay");

            var isValidated = await azureADJwtBearerValidation.ValidateTokenAsync(req.Headers);
            if (!isValidated)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                return unauthorizedResponse;
            }

            UrlClickStatsRequest input;
            var result = new ClickDateList();

            if (req == null)
                return req.CreateResponse(HttpStatusCode.NotFound);

            try
            {
                using (var reader = new StreamReader(req.Body))
                {
                    var strBody = await reader.ReadToEndAsync();
                    input = JsonSerializer.Deserialize<UrlClickStatsRequest>(strBody, s_readOptions);
                    if (input == null)
                        return req.CreateResponse(HttpStatusCode.NotFound);
                }

                var rawStats = await storageTableHelper.GetAllStatsByVanityAsync(input.Vanity);
                result.Items = rawStats
                    .GroupBy(s => DateTime.Parse(s.Datetime).Date)
                    .Select(stat => new ClickDate
                    {
                        DateClicked = stat.Key.ToString("yyyy-MM-dd"),
                        Count = stat.Count()
                    })
                    .OrderBy(s => DateTime.Parse(s.DateClicked).Date)
                    .ToList();

                var host = string.IsNullOrEmpty(_settings.CustomDomain) ? req.Url.Host : _settings.CustomDomain.ToString();
                result.Url = Utility.GetShortUrl(host, input.Vanity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error was encountered.");
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteAsJsonAsync(new { Message = $"{ex.Message}" });
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
        // [Required] the end of the URL that you want statistics for.
        "vanity": "azFunc"
    }

Output:
    {
    "items": [
        {
        "dateClicked": "2020-12-19",
        "count": 1
        },
        {
        "dateClicked": "2020-12-03",
        "count": 2
        }
    ],
    "url": ""https://c5m.ca/29"
*/