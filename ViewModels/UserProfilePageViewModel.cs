namespace StudentEnrollmentSystem.ViewModels;

public class UserProfilePageViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string? StudentNumber { get; set; }
    public string? ProgramName { get; set; }
    public string? IntakeLabel { get; set; }
    public string? CurrentSemesterName { get; set; }
    public string BankAccountMask { get; set; } = "Not added";
    public string ActiveSection { get; set; } = "profile";
    public ProfileInfoInputModel Profile { get; set; } = new();
    public BankDetailsInputModel BankDetails { get; set; } = new();
    public ChangePasswordInputModel Password { get; set; } = new();
}
