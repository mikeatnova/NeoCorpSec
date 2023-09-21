﻿using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

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

        [MaxLength(256)]
        public string? Notes { get; set; }

        [Required]
        [MaxLength(20)]
        public string CurrentStatus { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? ModifiedAt { get; set; }

        public List<CameraLocation> CameraLocations { get; set; }
    }
}
