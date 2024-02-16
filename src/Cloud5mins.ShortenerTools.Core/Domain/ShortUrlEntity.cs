using System.Runtime.Serialization;
using System.Text.Json;
using Azure;
using Azure.Data.Tables;

namespace Cloud5mins.ShortenerTools.Core.Domain
{
    public class ShortUrlEntity : ITableEntity
    {
        public ShortUrlEntity() { }
        public ShortUrlEntity(string longUrl, string endUrl)
        {
            Initialize(longUrl, endUrl, string.Empty, null);
        }
        public ShortUrlEntity(string longUrl, string endUrl, Schedule[] schedules)
        {
            Initialize(longUrl, endUrl, string.Empty, schedules);
        }
        public ShortUrlEntity(string longUrl, string endUrl, string title, Schedule[] schedules)
        {
            Initialize(longUrl, endUrl, title, schedules);
        }

        #region ITableEntity Members
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        #endregion

        public string Url { get; set; }
        private string _activeUrl;
        public string ActiveUrl
        {
            get
            {
                if (string.IsNullOrEmpty(_activeUrl))
                    if (Schedules != null)
                        _activeUrl = GetActiveUrl(DateTime.UtcNow);
                    else
                        _activeUrl = Url;

                return _activeUrl;
            }
        }
        public string Title { get; set; }
        public string ShortUrl { get; set; }
        public int Clicks { get; set; }
        public bool? IsArchived { get; set; }
        public string SchedulesPropertyRaw { get; set; }

        private List<Schedule> _schedules;
        [IgnoreDataMember]
        public List<Schedule> Schedules
        {
            get
            {
                if (_schedules == null)
                    if (string.IsNullOrEmpty(SchedulesPropertyRaw))
                        _schedules = new List<Schedule>();
                    else
                        _schedules = JsonSerializer.Deserialize<Schedule[]>(SchedulesPropertyRaw).ToList();
                return _schedules;
            }
            set
            {
                _schedules = value;
            }
        }

        public static ShortUrlEntity GetEntity(string longUrl, string endUrl, string title, Schedule[] schedules)
        {
            return new ShortUrlEntity
            {
                PartitionKey = endUrl.First().ToString(),
                RowKey = endUrl,
                Url = longUrl,
                Title = title,
                Schedules = schedules.ToList()
            };
        }

        private void Initialize(string longUrl, string endUrl, string title, Schedule[] schedules)
        {
            PartitionKey = endUrl.First().ToString();
            RowKey = endUrl;
            Url = longUrl;
            Title = title;
            Clicks = 0;
            IsArchived = false;

            if (schedules?.Length > 0)
            {
                Schedules = schedules.ToList();
                SchedulesPropertyRaw = JsonSerializer.Serialize(Schedules);
            }
        }

        private string GetActiveUrl(DateTime pointInTime)
        {
            var link = Url;
            var active = Schedules.Where(s =>
                s.End > pointInTime && //hasn't ended
                s.Start < pointInTime //already started
                ).OrderBy(s => s.Start); //order by start to process first link

            foreach (var sched in active.ToArray())
            {
                if (sched.IsActive(pointInTime))
                {
                    link = sched.AlternativeUrl;
                    break;
                }
            }
            return link;
        }
    }

}
