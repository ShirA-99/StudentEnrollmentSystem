namespace StudentEnrollmentSystem.ViewModels;

public class EnrollmentIndexViewModel
{
    public string StudentName { get; set; } = string.Empty;

    public string StudentNumber { get; set; } = string.Empty;

    public string ProgramName { get; set; } = string.Empty;

    public string IntakeLabel { get; set; } = string.Empty;

    public string SemesterName { get; set; } = string.Empty;

    public string RegistrationWindow { get; set; } = string.Empty;

    public int ActiveEnrollmentCount { get; set; }

    public int CurrentCreditHours { get; set; }

    public int AvailableSectionCount { get; set; }

    public string RecentActivityText { get; set; } = string.Empty;

    public IReadOnlyList<AvailableSectionViewModel> AvailableSections { get; set; } = [];
}
