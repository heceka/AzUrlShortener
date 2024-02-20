using System.Net;
using Cloud5mins.ShortenerTools.Core.Domain;
using Cloud5mins.ShortenerTools.Core.Domain.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Cloud5mins.ShortenerTools.Functions
{
    public class UrlRedirect
    {
        private readonly ILogger _logger;
        private readonly ShortenerSettings _settings;

        public UrlRedirect(ILoggerFactory loggerFactory, ShortenerSettings settings)
        {
            _logger = loggerFactory.CreateLogger<UrlRedirect>();
            _settings = settings;
        }

        [Function("UrlRedirect")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Redirect/{shortUrl}")] HttpRequestData req,
            string shortUrl,
            ExecutionContext context)
        {
            string redirectUrl = "https://azure.com";

            if (!string.IsNullOrWhiteSpace(shortUrl))
            {
                redirectUrl = _settings.DefaultRedirectUrl ?? redirectUrl;

                StorageTableHelper stgHelper = new StorageTableHelper(_settings.DataStorage);

                var tempUrl = new ShortUrlEntity(string.Empty, shortUrl);
                var newUrl = await stgHelper.GetShortUrlEntityAsync(tempUrl);

                if (newUrl != null)
                {
                    _logger.LogInformation($"Found it: {newUrl.Url}");
                    newUrl.Clicks++;
                    await stgHelper.SaveClickStatsEntityAsync(new ClickStatsEntity(newUrl.RowKey));
                    await stgHelper.SaveShortUrlEntityAsync(newUrl);
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
