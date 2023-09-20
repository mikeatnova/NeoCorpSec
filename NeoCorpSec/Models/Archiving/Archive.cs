using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace NeoCorpSec.Models.Archiving
{
    [Table("Archives")]
    public class Archive
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        public DateTime ArchivedAt { get; set; }

        [ForeignKey("Shift")]
        public int ShiftId { get; set; }

        [ForeignKey("Tour")]
        public int TourId { get; set; }
        [Required]
        public DateTime RetentionDate { get; set; }
    }
}
