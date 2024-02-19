using System.Collections.Generic;
using Cloud5mins.ShortenerTools.Core.Domain.Models;

namespace Cloud5mins.ShortenerTools.Core.Messages
{
    public class ListResponse
    {
        public IList<ShortUrlEntity> UrlList { get; set; }

        public ListResponse() { }
        public ListResponse(IList<ShortUrlEntity> list)
        {
            UrlList = list;
        }
    }
}