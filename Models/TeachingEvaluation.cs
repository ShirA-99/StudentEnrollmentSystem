using System;
using System.ComponentModel.DataAnnotations;

namespace StudentEnrollmentSystem.Models
{
    public class TeachingEvaluation
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Student Name")]
        public string StudentName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Display(Name = "Student ID")]
        public string StudentId { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Display(Name = "Course Code")]
        public string CourseCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Lecturer Name")]
        public string LecturerName { get; set; } = string.Empty;

        [Required]
        [Range(1, 5)]
        [Display(Name = "Teaching Clarity")]
        public int TeachingClarity { get; set; }

        [Required]
        [Range(1, 5)]
        public int Preparedness { get; set; }

        [Required]
        [Range(1, 5)]
        public int Engagement { get; set; }

        [StringLength(1000)]
        public string? Comments { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.Now;
    }
}