using NeoCorpSec.Models.Authenitcation;
using NeoCorpSec.Models.ShiftManagement;
using NeoCorpSec.Models.TourManagement;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;


namespace NeoCorpSec.Models.Reporting
{

    [Table("Notes")]
    public class Note
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        [ForeignKey("SecurityUser")]
        public int UserId { get; set; }

        public virtual SecurityUser User { get; set; }

        [Required]
        public string Content { get; set; }

        public string? Username { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? Role { get; set; }

        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }
    }
}   