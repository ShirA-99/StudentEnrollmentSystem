using System.ComponentModel.DataAnnotations;

namespace StudentEnrollmentSystem.Models;

public class CourseSection
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public Course Course { get; set; } = null!;

    public int SemesterId { get; set; }

    public Semester Semester { get; set; } = null!;

    [Required]
    [StringLength(20)]
    public string SectionCode { get; set; } = string.Empty;

    [Range(1, 300)]
    public int Capacity { get; set; }

    [Required]
    [StringLength(120)]
    public string InstructorName { get; set; } = string.Empty;

    public ICollection<SectionMeeting> Meetings { get; set; } = new List<SectionMeeting>();

    public ICollection<EnrollmentRecord> EnrollmentRecords { get; set; } = new List<EnrollmentRecord>();

    public ICollection<AddDropAudit> AddDropAudits { get; set; } = new List<AddDropAudit>();
}
