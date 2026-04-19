namespace StudentEnrollmentSystem.ViewModels;

public class HomeIndexViewModel
{
    public string PortalTitle { get; set; } = string.Empty;

    public string SemesterName { get; set; } = string.Empty;

    public string EnrollmentWindow { get; set; } = string.Empty;

    public int CatalogCourseCount { get; set; }

    public int OpenSectionCount { get; set; }

    public int ActiveStudentCount { get; set; }

    public int CurrentRegistrationCount { get; set; }

    public string RecentUpdateText { get; set; } = string.Empty;

    public IReadOnlyList<DemoLoginViewModel> DemoAccounts { get; set; } = [];
}

public class DemoLoginViewModel
{
    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string ProgramName { get; set; } = string.Empty;

    public string SemesterName { get; set; } = string.Empty;
}
