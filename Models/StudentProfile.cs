using System.ComponentModel.DataAnnotations;

namespace StudentEnrollmentSystem.Models;

public class StudentProfile
{
    public int Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser ApplicationUser { get; set; } = null!;

    [Required]
    [StringLength(20)]
    public string StudentNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string ProgramName { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string ProgramCode { get; set; } = string.Empty;

    [Required]
    [StringLength(40)]
    public string IntakeLabel { get; set; } = string.Empty;

    public int? CurrentSemesterId { get; set; }

    public Semester? CurrentSemester { get; set; }

    public ICollection<EnrollmentRecord> EnrollmentRecords { get; set; } = new List<EnrollmentRecord>();

    public ICollection<AddDropAudit> AddDropAudits { get; set; } = new List<AddDropAudit>();
}
