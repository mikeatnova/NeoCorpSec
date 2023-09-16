using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace NeoCorpSec.Models.TourManagement
{
    [Table("TourNotes")]
    public class TourNote
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [ForeignKey("Tour")]
        public int TourId { get; set; }

        [Required]
        public string Note { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }
    }
}
