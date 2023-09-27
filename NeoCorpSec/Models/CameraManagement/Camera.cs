using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using NeoCorpSec.Models.Reporting;
using NeoCorpSec.Models.CameraManagement;

namespace NeoCorpSec.Models.CameraManagement
{
    [Table("Cameras")]
    public class Camera
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [ForeignKey("Location")]
        public int LocationId { get; set; }

        [Required]
        [MaxLength(20)]
        public string CurrentStatus { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedAt { get; set; }

        public List<CameraLocation> CameraLocations { get; set; }
        public ICollection<Note> Notes { get; set; }
    }
}
