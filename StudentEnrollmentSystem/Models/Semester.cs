using System.ComponentModel.DataAnnotations;

namespace StudentEnrollmentSystem.Models;

public class Semester
{
    public int Id { get; set; }

    [Required]
    [StringLength(30)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string Name { get; set; } = string.Empty;

    public DateOnly EnrollmentStartDate { get; set; }

    public DateOnly EnrollmentEndDate { get; set; }

    public SemesterStatus Status { get; set; }

    public ICollection<CourseSection> CourseSections { get; set; } = new List<CourseSection>();
}
