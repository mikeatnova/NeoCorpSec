using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using NeoCorpSec.Models.Reporting;

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

        public virtual Tour Tour { get; set; }

        [ForeignKey("Note")]
        public int? NoteId { get; set; }
        public Note Note { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }
    }
}
