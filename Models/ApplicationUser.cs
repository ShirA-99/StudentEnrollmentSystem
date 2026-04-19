using Microsoft.AspNetCore.Identity;

namespace StudentEnrollmentSystem.Models;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Postcode { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? BankName { get; set; }
    public string? EncryptedBankAccountNumber { get; set; }
    public string? EncryptedBankAccountHolderName { get; set; }

    public StudentProfile? StudentProfile { get; set; }
}
