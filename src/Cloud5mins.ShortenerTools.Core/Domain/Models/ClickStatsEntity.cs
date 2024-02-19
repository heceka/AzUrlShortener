using Azure;
using Azure.Data.Tables;

namespace Cloud5mins.ShortenerTools.Core.Domain.Models
{
    public class ClickStatsEntity : ITableEntity
    {
        public ClickStatsEntity() { }
        public ClickStatsEntity(string vanity)
        {
            PartitionKey = vanity;
            RowKey = Guid.NewGuid().ToString();
            Datetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        }

        #region ITableEntity Members
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        #endregion

        //public string Id { get; set; }
        public string Datetime { get; set; }
    }
}
