using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentEnrollmentSystem.Data;
using StudentEnrollmentSystem.Models;
using StudentEnrollmentSystem.ViewModels;

namespace StudentEnrollmentSystem.Controllers;

public class EnquiryController : Controller
{
    private readonly ApplicationDbContext _context;

    public EnquiryController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult ContactUs()
    {
        return View(new ContactUsMessage());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ContactUs(ContactUsMessage model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        model.SubmittedAt = DateTime.UtcNow;
        _context.ContactUsMessages.Add(model);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(ContactUsSuccess));
    }

    [HttpGet]
    public IActionResult ContactUsSuccess()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TimetableMatching(TimetableMatchingViewModel model)
    {
        var freshData = await _context.SectionMeetings
            .Include(m => m.CourseSection)
            .ThenInclude(cs => cs.Course)
            .Take(20)
            .Select(m => new TimetableOptionViewModel
            {
                MeetingId = m.Id,
                CourseSectionId = m.CourseSectionId,
                CourseCode = m.CourseSection.Course.Code,
                CourseTitle = m.CourseSection.Course.Title,
                SectionCode = m.CourseSection.SectionCode,
                InstructorName = m.CourseSection.InstructorName,
                DayOfWeek = m.DayOfWeek,
                StartTime = m.StartTime,
                EndTime = m.EndTime,
                Venue = m.Venue,
                IsSelected = false
            })
            .ToListAsync();

        foreach (var item in freshData)
        {
            var selected = model.AvailableSections
                .FirstOrDefault(x => x.MeetingId == item.MeetingId);

            item.IsSelected = selected?.IsSelected ?? false;
        }

        model.AvailableSections = freshData;
        model.HasChecked = true;

        var selectedMeetings = model.AvailableSections
            .Where(x => x.IsSelected)
            .ToList();

        if (!selectedMeetings.Any())
        {
            model.ClashMessages.Add("Please select at least one timetable entry.");
            return View(model);
        }

        for (int i = 0; i < selectedMeetings.Count; i++)
        {
            for (int j = i + 1; j < selectedMeetings.Count; j++)
            {
                var a = selectedMeetings[i];
                var b = selectedMeetings[j];

                if (a.DayOfWeek == b.DayOfWeek &&
                    a.StartTime < b.EndTime &&
                    b.StartTime < a.EndTime)
                {
                    model.ClashMessages.Add(
                        $"Clash detected between {a.CourseCode} ({a.SectionCode}) and {b.CourseCode} ({b.SectionCode}) on {a.DayOfWeek}."
                    );
                }
            }
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> TimetableMatching()
    {
        var meetings = await _context.SectionMeetings
            .Include(m => m.CourseSection)
            .ThenInclude(cs => cs.Course)
            .Take(40)
            .ToListAsync();

        var model = new TimetableMatchingViewModel
        {
            AvailableSections = meetings.Select(m => new TimetableOptionViewModel
            {
                MeetingId = m.Id,
                CourseSectionId = m.CourseSectionId,
                CourseCode = m.CourseSection.Course.Code,
                CourseTitle = m.CourseSection.Course.Title,
                SectionCode = m.CourseSection.SectionCode,
                InstructorName = m.CourseSection.InstructorName,
                DayOfWeek = m.DayOfWeek,
                StartTime = m.StartTime,
                EndTime = m.EndTime,
                Venue = m.Venue
            }).ToList()
        };

        ViewBag.DebugCount = model.AvailableSections.Count;
        return View(model);
    }
}
