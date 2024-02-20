namespace Cloud5mins.ShortenerTools.Functions.Utils
{
    class Constants
    {
        internal struct Authorization
        {
            internal struct Header
            {
                internal const string Scheme = "Authorization-Scheme";
                internal const string Name = "Authorization";
                internal const string Type = "string";
                internal const string Desc = "Token value";
                internal const string FunctionsKey = "x-functions-key";
            }

            internal struct Schemes
            {
                internal const string Bearer = "Bearer";
            }
        }
    }
}
