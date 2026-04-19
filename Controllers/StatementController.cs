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
                .Include(s => s.CurrentSemester)
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

            ViewBag.CurrentSemesterId = student.CurrentSemesterId;
            ViewBag.ActiveSemester = student.CurrentSemester?.Name ?? "Current Semester";

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
                .Include(s => s.CurrentSemester)
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

            ViewBag.CurrentSemesterId = student.CurrentSemesterId;
            ViewBag.ActiveSemester = student.CurrentSemester?.Name ?? "Current Semester";
            ViewBag.ClashOnly = clashOnly;

            return View(student);
        }
    }
}