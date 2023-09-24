﻿using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using NeoCorpSec.Models.Reporting;

namespace NeoCorpSec.Models.CameraManagment
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

        [ForeignKey("Note")]
        public int? NoteId { get; set; }
        public Note Note { get; set; }

        [Required]
        [MaxLength(20)]
        public string CurrentStatus { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? ModifiedAt { get; set; }

        public List<CameraLocation> CameraLocations { get; set; }
    }
}
