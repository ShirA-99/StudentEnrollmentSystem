using Microsoft.AspNetCore.Mvc;
using StudentEnrollmentSystem.Data;
using StudentEnrollmentSystem.Models;

namespace StudentEnrollmentSystem.Controllers
{
    public class EvaluationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EvaluationController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TeachingEvaluation model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            _context.TeachingEvaluations.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ThankYou));
        }

        public IActionResult ThankYou()
        {
            return View();
        }
    }
}