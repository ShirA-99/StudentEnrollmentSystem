namespace StudentEnrollmentSystem.ViewModels;

public class AddDropHistoryViewModel
{
    public string StudentName { get; set; } = string.Empty;

    public IReadOnlyList<AddDropHistoryItemViewModel> HistoryItems { get; set; } = [];
}
