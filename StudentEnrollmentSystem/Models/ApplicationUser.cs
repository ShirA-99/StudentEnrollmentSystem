using Microsoft.AspNetCore.Identity;

namespace StudentEnrollmentSystem.Models;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;

    public StudentProfile? StudentProfile { get; set; }
}
