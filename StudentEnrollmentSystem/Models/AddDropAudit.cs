using System.ComponentModel.DataAnnotations;

namespace StudentEnrollmentSystem.Models;

public class AddDropAudit
{
    public int Id { get; set; }

    public int StudentProfileId { get; set; }

    public StudentProfile StudentProfile { get; set; } = null!;

    public int CourseSectionId { get; set; }

    public CourseSection CourseSection { get; set; } = null!;

    public AddDropActionType ActionType { get; set; }

    public DateTime ActionAtUtc { get; set; }

    [StringLength(300)]
    public string? Remarks { get; set; }
}
