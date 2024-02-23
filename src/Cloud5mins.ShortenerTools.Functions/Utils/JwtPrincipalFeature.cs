﻿using System.Security.Claims;

namespace Cloud5mins.ShortenerTools.Functions.Utils
{
    /// <summary>
    /// Holds the authenticated user principal
    /// for the request along with the
    /// access token they used.
    /// </summary>
    internal class JwtPrincipalFeature
    {
        public JwtPrincipalFeature(ClaimsPrincipal principal, string accessToken)
        {
            Principal = principal;
            AccessToken = accessToken;
        }

        public ClaimsPrincipal Principal { get; }

        /// <summary>
        /// The access token that was used for this
        /// request. Can be used to acquire further
        /// access tokens with the on-behalf-of flow.
        /// </summary>
        public string AccessToken { get; }
    }
}
