﻿using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace NeoCorpSec.Models.Authenitcation
{
    [Table("SecurityUsers")]
    public class SecurityUser
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string ID { get; set; }

        [MaxLength(50)]
        public string? FirstName { get; set; }

        [MaxLength(50)]
        public string? LastName { get; set; }
        
        [MaxLength(50)]
        public string? SecurityUsername { get; set; }

        [Required]
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime ModifiedAt { get; set; }
        public DateTime DeletedAt { get; set; }

        public DateTime? HiredDate { get; set; }

        public DateTime? TerminationDate { get; set; }

        // Navigation property for 1-to-1 relationship with IdentityUser
        [ForeignKey("IdentityUser")]
        public string? IdentityUserId { get; set; }

        public IdentityUser IdentityUser { get; set; }
    }
}
