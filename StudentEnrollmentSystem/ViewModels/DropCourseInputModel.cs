using System.ComponentModel.DataAnnotations;

namespace StudentEnrollmentSystem.ViewModels;

public class DropCourseInputModel
{
    public int EnrollmentId { get; set; }

    [Required]
    [StringLength(300)]
    public string Remarks { get; set; } = string.Empty;
}
