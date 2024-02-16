using System.ComponentModel.DataAnnotations;
using Cloud5mins.ShortenerTools.Core.Domain;

namespace Cloud5mins.ShortenerTools.Core.Messages
{
    public class ShortUrlRequest
    {
        public string Title { get; set; }

        private string _vanity;
        public string Vanity
        {
            get
            {
                return _vanity ?? string.Empty;
            }
            set
            {
                _vanity = value;
            }
        }

        [Required]
        public string Url { get; set; }

        private List<Schedule> _schedules;

        public List<Schedule> Schedules
        {
            get
            {
                if (_schedules == null)
                {
                    _schedules = new List<Schedule>();
                }
                return _schedules;
            }
            set
            {
                _schedules = value;
            }
        }
    }
}