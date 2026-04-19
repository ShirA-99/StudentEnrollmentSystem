namespace StudentEnrollmentSystem.ViewModels;

public class AddDropIndexViewModel
{
    public string StudentName { get; set; } = string.Empty;

    public string StudentNumber { get; set; } = string.Empty;

    public string ProgramName { get; set; } = string.Empty;

    public string SemesterName { get; set; } = string.Empty;

    public string RegistrationWindow { get; set; } = string.Empty;

    public int ActiveEnrollmentCount { get; set; }

    public int CurrentCreditHours { get; set; }

    public int AvailableSectionCount { get; set; }

    public int TotalHistoryEvents { get; set; }

    public string RecentActivityText { get; set; } = string.Empty;

    public bool IsRegistrationOpen { get; set; }

    public string RegistrationStatusMessage { get; set; } = string.Empty;

    public IReadOnlyList<CurrentEnrollmentViewModel> CurrentEnrollments { get; set; } = [];

    public IReadOnlyList<AvailableSectionViewModel> AvailableSections { get; set; } = [];
}
