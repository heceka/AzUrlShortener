using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text.Json;
using Cloud5mins.ShortenerTools.Functions.Configurations;
using Cloud5mins.ShortenerTools.Functions.Extensions;
using Cloud5mins.ShortenerTools.Functions.Utils;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using static Cloud5mins.ShortenerTools.Functions.Utils.Constants;

namespace Cloud5mins.ShortenerTools.Functions.Middlewares
{
    internal class AuthenticationMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly ILogger<AuthenticationMiddleware> _logger;
        private readonly JwtSecurityTokenHandler _tokenValidator;
        private readonly ConfigurationManager<OpenIdConnectConfiguration> _configurationManager;
        private readonly AzureAuthenticationOptions _authenticationOptions;

        public AuthenticationMiddleware(ILoggerFactory loggerFactory, IOptions<AzureAuthenticationOptions> options)
        {
            _logger = loggerFactory.CreateLogger<AuthenticationMiddleware>();
            _authenticationOptions = options.Value;
            _tokenValidator = new JwtSecurityTokenHandler();
            _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(_authenticationOptions.MetadataAddress, new OpenIdConnectConfigurationRetriever());
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            if (!TryGetTokenFromHeaders(context, out var token))
            {
                // Unable to get token from headers
                await context.SetHttpResponseStatusCodeAsync(HttpStatusCode.Unauthorized);
                return;
            }

            if (!_tokenValidator.CanReadToken(token))
            {
                // Token is malformed
                await context.SetHttpResponseStatusCodeAsync(HttpStatusCode.Unauthorized);
                return;
            }

            try
            {
                var openIdConfig = await _configurationManager.GetConfigurationAsync(default);
                // Get OpenID Connect metadata
                TokenValidationParameters tokenValidationParameters = new()
                {
                    ValidAudience = _authenticationOptions.ClientId,
                    IssuerSigningKeys = openIdConfig.SigningKeys,
                    ValidIssuer = openIdConfig.Issuer
                };

                try
                {
                    // Validate token                    
                    var principal = _tokenValidator.ValidateToken(token, tokenValidationParameters, out _);

                    if (!principal.Identity.IsAuthenticated)
                    {
                        await context.SetHttpResponseStatusCodeAsync(HttpStatusCode.Unauthorized);
                        return;
                    }

                    // Set principal + token in Features collection
                    // They can be accessed from here later in the call chain
                    context.Features.Set(new JwtPrincipalFeature(principal, token));

                    await next(context);
                }
                catch (SecurityTokenException stex)
                {
                    // Token is not valid (expired etc.)
                    _logger.LogError(stex, stex.Message);
                    await context.SetHttpResponseStatusCodeAsync(HttpStatusCode.Unauthorized);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                await context.SetHttpResponseStatusCodeAsync(HttpStatusCode.InternalServerError);
                return;
            }
        }

        private static bool TryGetTokenFromHeaders(FunctionContext context, out string token)
        {
            token = null;
            // HTTP headers are in the binding context as a JSON object
            // The first checks ensure that we have the JSON string
            if (!context.BindingContext.BindingData.TryGetValue(Authorizations.Headers.HeadersKey, out var headersObj))
                return false;

            if (headersObj is not string headersStr)
                return false;

            var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(headersStr);
            var normalizedKeyHeaders = headers.ToDictionary(h => h.Key.ToLowerInvariant(), h => h.Value);
            if (!normalizedKeyHeaders.TryGetValue(Authorizations.Headers.Name.ToLower(), out var authHeaderValue))
                // No Authorization header present
                return false;

            if (!authHeaderValue.StartsWith(Authorizations.Schemes.Bearer, StringComparison.OrdinalIgnoreCase))
                // Scheme is not Bearer
                return false;

            token = authHeaderValue.Substring(Authorizations.Schemes.Bearer.Length).Trim();
            return true;
        }
    }
}
