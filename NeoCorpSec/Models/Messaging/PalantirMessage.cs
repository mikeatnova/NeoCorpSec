using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace NeoCorpSec.Models.Messaging
{
    public class PalantirMessage
    {

        [Required]
        public string Title { get; set; }

        [Required]
        public string MessageBody { get; set; }

        [Required]
        public string Username { get; set; }
        [Required]
        public string Status { get; set; }

        [Required]
        public string Realm { get; set; }

        [Required]
        [Range(1, 5)]
        public int UrgencyRating { get; set; }
    }
}
