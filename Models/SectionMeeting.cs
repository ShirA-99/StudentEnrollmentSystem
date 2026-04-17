using System.ComponentModel.DataAnnotations;

namespace StudentEnrollmentSystem.Models;

public class SectionMeeting
{
    public int Id { get; set; }

    public int CourseSectionId { get; set; }

    public CourseSection CourseSection { get; set; } = null!;

    public DayOfWeek DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    [Required]
    [StringLength(80)]
    public string Venue { get; set; } = string.Empty;
}
