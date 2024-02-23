namespace Cloud5mins.ShortenerTools.Functions.Utils
{
    class Constants
    {
        internal struct ConfigKeys
        {
            internal const string ShortenerSettings = "ShortenerSettings";
            internal const string AzureAuthenticationOptions = "AzureAuthenticationOptions";
        }

        internal struct Authorizations
        {
            internal struct Headers
            {
                internal const string HeadersKey = "Headers";
                internal const string Name = "Authorization";
                internal const string ScopeClaimType = "http://schemas.microsoft.com/identity/claims/scope";
                //internal const string Scheme = "Authorization-Scheme";
                //internal const string Type = "string";
                //internal const string Desc = "Token value";
                //internal const string FunctionsKey = "x-functions-key";
            }

            internal struct Schemes
            {
                internal const string Bearer = "Bearer";
            }

            internal struct OpenApiSecurity
            {
                internal const string FunctionKey = "function_key";
            }
        }
    }
}
