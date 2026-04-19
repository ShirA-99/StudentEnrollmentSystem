using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentEnrollmentSystem.Data;
using StudentEnrollmentSystem.Models;
using System.Security.Claims;

namespace StudentEnrollmentSystem.Controllers
{
    public class StatementController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StatementController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var student = await _context.StudentProfiles
                .Include(s => s.EnrollmentRecords)
                    .ThenInclude(e => e.CourseSection)
                        .ThenInclude(cs => cs.Course)
                .Include(s => s.EnrollmentRecords)
                    .ThenInclude(e => e.CourseSection)
                        .ThenInclude(cs => cs.Semester)
                .FirstOrDefaultAsync(s => s.ApplicationUserId == userId);

            if (student == null)
            {
                return NotFound("Student profile not found.");
            }

            var currentSemesterId = student.EnrollmentRecords
                .Where(e => e.Status == EnrollmentStatus.Enrolled)
                .Select(e => e.CourseSection.SemesterId)
                .DefaultIfEmpty(0)
                .Max();

            ViewBag.CurrentSemesterId = currentSemesterId;
            ViewBag.ActiveSemester = student.EnrollmentRecords
                .Where(e => e.Status == EnrollmentStatus.Enrolled && e.CourseSection.SemesterId == currentSemesterId)
                .Select(e => e.CourseSection.Semester)
                .FirstOrDefault();

            return View(student);
        }

        public async Task<IActionResult> Timetable(bool clashOnly = false)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var student = await _context.StudentProfiles
                .Include(s => s.EnrollmentRecords)
                    .ThenInclude(e => e.CourseSection)
                        .ThenInclude(cs => cs.Meetings)
                .Include(s => s.EnrollmentRecords)
                    .ThenInclude(e => e.CourseSection)
                        .ThenInclude(cs => cs.Course)
                .Include(s => s.EnrollmentRecords)
                    .ThenInclude(e => e.CourseSection)
                        .ThenInclude(cs => cs.Semester)
                .FirstOrDefaultAsync(s => s.ApplicationUserId == userId);

            if (student == null)
            {
                return NotFound("Student profile not found.");
            }

            var currentSemesterId = student.EnrollmentRecords
                .Where(e => e.Status == EnrollmentStatus.Enrolled)
                .Select(e => e.CourseSection.SemesterId)
                .DefaultIfEmpty(0)
                .Max();

            ViewBag.CurrentSemesterId = currentSemesterId;
            ViewBag.ActiveSemester = student.EnrollmentRecords
                .Where(e => e.Status == EnrollmentStatus.Enrolled && e.CourseSection.SemesterId == currentSemesterId)
                .Select(e => e.CourseSection.Semester)
                .FirstOrDefault();

            ViewBag.ClashOnly = clashOnly;

            return View(student);
        }
    }
}