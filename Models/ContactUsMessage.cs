using System;
using System.ComponentModel.DataAnnotations;

namespace StudentEnrollmentSystem.Models
{
    public class ContactUsMessage
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        public DateTime SubmittedAt { get; set; } = DateTime.Now;
    }
}