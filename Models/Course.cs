using System.ComponentModel.DataAnnotations;

namespace StudentEnrollmentSystem.Models;

public class Course
{
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Range(1, 10)]
    public int CreditHours { get; set; }

    public ICollection<CourseSection> Sections { get; set; } = new List<CourseSection>();
}
