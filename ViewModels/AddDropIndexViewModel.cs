namespace StudentEnrollmentSystem.ViewModels;

public class AddDropIndexViewModel
{
    public string StudentName { get; set; } = string.Empty;

    public string SemesterName { get; set; } = string.Empty;

    public IReadOnlyList<CurrentEnrollmentViewModel> CurrentEnrollments { get; set; } = [];

    public IReadOnlyList<AvailableSectionViewModel> AvailableSections { get; set; } = [];
}
