using System.ComponentModel.DataAnnotations;

namespace StudentEnrollmentSystem.ViewModels;

public class BankDetailsInputModel
{
    [Required]
    [Display(Name = "Bank Name")]
    [StringLength(120)]
    public string BankName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Account Number")]
    [RegularExpression(@"^[0-9]{8,20}$", ErrorMessage = "Account number must contain 8 to 20 digits.")]
    public string AccountNumber { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Account Holder Name")]
    [StringLength(120)]
    public string AccountHolderName { get; set; } = string.Empty;
}
