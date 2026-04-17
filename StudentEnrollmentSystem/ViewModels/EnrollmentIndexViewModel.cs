namespace StudentEnrollmentSystem.ViewModels;

public class EnrollmentIndexViewModel
{
    public string StudentName { get; set; } = string.Empty;

    public string StudentNumber { get; set; } = string.Empty;

    public string SemesterName { get; set; } = string.Empty;

    public string RegistrationWindow { get; set; } = string.Empty;

    public IReadOnlyList<AvailableSectionViewModel> AvailableSections { get; set; } = [];
}
