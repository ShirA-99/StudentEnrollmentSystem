using System.ComponentModel.DataAnnotations;

namespace StudentEnrollmentSystem.ViewModels;

public class ProfileInfoInputModel
{
    [Required]
    [Display(Name = "Full Name")]
    [StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Phone Number")]
    [RegularExpression(@"^\+?[0-9\s\-]{8,20}$", ErrorMessage = "Enter a valid phone number using digits, spaces, or hyphens.")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Address")]
    [StringLength(200)]
    public string? Address { get; set; }

    [Display(Name = "Postcode")]
    [StringLength(20)]
    public string? Postcode { get; set; }

    [Display(Name = "City")]
    [StringLength(100)]
    public string? City { get; set; }

    [Display(Name = "State")]
    [StringLength(100)]
    public string? State { get; set; }

    [Display(Name = "Country")]
    [StringLength(100)]
    public string? Country { get; set; }
}
