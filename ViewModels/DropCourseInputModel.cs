using System.ComponentModel.DataAnnotations;

namespace StudentEnrollmentSystem.ViewModels;

public class DropCourseInputModel
{
    public int EnrollmentId { get; set; }

    [Required(ErrorMessage = "A drop reason is required before removing a course.")]
    [StringLength(300, ErrorMessage = "Drop reasons must be 300 characters or fewer.")]
    public string Remarks { get; set; } = string.Empty;
}
