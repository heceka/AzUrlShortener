using Azure;
using Azure.Data.Tables;

namespace Cloud5mins.ShortenerTools.Core.Domain
{
    public class NextId : ITableEntity
    {
        #region ITableEntity Members
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        #endregion

        public int Id { get; set; }
    }
}
