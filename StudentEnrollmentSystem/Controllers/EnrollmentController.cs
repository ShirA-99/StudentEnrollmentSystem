using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentEnrollmentSystem.Services;

namespace StudentEnrollmentSystem.Controllers;

[Authorize]
public class EnrollmentController(EnrollmentService enrollmentService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var model = await enrollmentService.GetEnrollmentCatalogAsync(GetUserId());
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enroll(int sectionId)
    {
        var result = await enrollmentService.EnrollAsync(GetUserId(), sectionId);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? throw new InvalidOperationException("The current user is not authenticated.");
    }
}
