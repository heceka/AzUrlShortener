namespace Cloud5mins.ShortenerTools.Functions.Utils
{
    class Constants
    {
        internal struct ConfigKeys
        {
            internal const string AzureWebJobsStorage = "AzureWebJobsStorage";
            internal const string AzureAdOptions = "AzureAdOptions";
            internal const string ShortenerSettings = "ShortenerSettings";
        }

        internal struct Authorizations
        {
            internal struct Headers
            {
                internal const string Authorization = "Authorization";
                internal const string ScopeClaimType = "http://schemas.microsoft.com/identity/claims/scope";
            }

            internal struct Schemes
            {
                internal const string Bearer = "Bearer";
                internal const string BearerFormat = "JWT";
            }
        }
    }
}
