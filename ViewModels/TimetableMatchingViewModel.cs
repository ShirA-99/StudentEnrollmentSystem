using System;
using System.Collections.Generic;

namespace StudentEnrollmentSystem.ViewModels
{
    public class TimetableOptionViewModel
    {
        public int MeetingId { get; set; }
        public int CourseSectionId { get; set; }

        public string CourseCode { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public string SectionCode { get; set; } = string.Empty;
        public string InstructorName { get; set; } = string.Empty;

        public DayOfWeek DayOfWeek { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public string Venue { get; set; } = string.Empty;

        public bool IsSelected { get; set; }
    }

    public class TimetableMatchingViewModel
    {
        public List<TimetableOptionViewModel> AvailableSections { get; set; } = new();
        public List<string> ClashMessages { get; set; } = new();
        public bool HasChecked { get; set; }
    }
}