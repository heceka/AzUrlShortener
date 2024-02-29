using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Cloud5mins.ShortenerTools.Functions.Configurations;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using static Cloud5mins.ShortenerTools.Functions.Utils.Constants;

namespace Cloud5mins.ShortenerTools.Functions.Utils
{
    internal class AzureADJwtBearerValidation
    {
        private const string _requiredScope = "";//"access_as_user";

        private readonly ILogger<AzureADJwtBearerValidation> _logger;
        private readonly AzureAdOptions _azureAdOptions;
        private readonly ConfigurationManager<OpenIdConnectConfiguration> _configurationManager;

        private ClaimsPrincipal _claimsPrincipal;

        public AzureADJwtBearerValidation(ILoggerFactory loggerFactory, IOptions<AzureAdOptions> options)
        {
            _logger = loggerFactory.CreateLogger<AzureADJwtBearerValidation>();
            _azureAdOptions = options.Value;
            _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(_azureAdOptions.WellKnownEndpoint, new OpenIdConnectConfigurationRetriever());
        }

        public async Task<bool> ValidateTokenAsync(HttpHeadersCollection headers)
        {
            var normalizedKeyHeaders = headers.ToDictionary(h => h.Key.ToLowerInvariant(), h => h.Value);
            if (!normalizedKeyHeaders.TryGetValue(Authorizations.Headers.Authorization.ToLower(), out var authHeaderValue))
                return false;

            var authorizationHeader = authHeaderValue.FirstOrDefault();
            if (string.IsNullOrEmpty(authorizationHeader))
                return false;

            if (!authorizationHeader.Contains("Bearer"))
                return false;

            var accessToken = authorizationHeader.Substring("Bearer ".Length);

            _logger.LogDebug($"Get OIDC well known endpoints {_azureAdOptions.WellKnownEndpoint}");

            var openIdConfig = await _configurationManager.GetConfigurationAsync();
            var tokenValidator = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                RequireSignedTokens = true,
                ValidAudience = _azureAdOptions.ClientId,
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                IssuerSigningKeys = openIdConfig.SigningKeys,
                ValidIssuer = openIdConfig.Issuer
            };

            try
            {
                _claimsPrincipal = tokenValidator.ValidateToken(accessToken, validationParameters, out _);
                if (_claimsPrincipal.Identity.IsAuthenticated && IsScopeValid(_requiredScope))
                    return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }

            return false;
        }

        public string GetPreferredUserName()
        {
            string preferredUsername = string.Empty;
            var preferred_username = _claimsPrincipal.Claims.FirstOrDefault(t => t.Type == "preferred_username");
            if (preferred_username != null)
            {
                preferredUsername = preferred_username.Value;
            }

            return preferredUsername;
        }

        private bool IsScopeValid(string scopeName)
        {
            if (_claimsPrincipal == null)
            {
                _logger.LogWarning($"Scope invalid {scopeName}");
                return false;
            }

            var scopeClaim = _claimsPrincipal.HasClaim(x => x.Type == Authorizations.Headers.ScopeClaimType)
                ? _claimsPrincipal.Claims.First(x => x.Type == Authorizations.Headers.ScopeClaimType).Value
                : string.Empty;

            if (!scopeClaim.Equals(scopeName, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning($"Scope invalid {scopeName}");
                return false;
            }

            _logger.LogDebug($"Scope valid {scopeName}");
            return true;
        }
    }
}
