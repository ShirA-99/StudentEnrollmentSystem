namespace StudentEnrollmentSystem.ViewModels;

public class AddDropHistoryViewModel
{
    public string StudentName { get; set; } = string.Empty;

    public string StudentNumber { get; set; } = string.Empty;

    public string ProgramName { get; set; } = string.Empty;

    public int TotalActions { get; set; }

    public int AddedCount { get; set; }

    public int DroppedCount { get; set; }

    public string LatestActivityText { get; set; } = string.Empty;

    public IReadOnlyList<AddDropHistoryItemViewModel> HistoryItems { get; set; } = [];
}
