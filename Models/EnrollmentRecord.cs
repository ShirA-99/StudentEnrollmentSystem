using System.ComponentModel.DataAnnotations;

namespace StudentEnrollmentSystem.Models;

public class EnrollmentRecord
{
    public int Id { get; set; }

    public int StudentProfileId { get; set; }

    public StudentProfile StudentProfile { get; set; } = null!;

    public int CourseSectionId { get; set; }

    public CourseSection CourseSection { get; set; } = null!;

    public EnrollmentStatus Status { get; set; }

    public DateTime EnrolledAtUtc { get; set; }

    public DateTime? DroppedAtUtc { get; set; }

    [StringLength(300)]
    public string? DropReason { get; set; }
}
