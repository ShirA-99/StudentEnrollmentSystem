using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentEnrollmentSystem.Services;
using StudentEnrollmentSystem.ViewModels;

namespace StudentEnrollmentSystem.Controllers;

[Authorize]
public class AddDropController(AddDropService addDropService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var model = await addDropService.GetDashboardAsync(GetUserId());
        return View(model);
    }

    public async Task<IActionResult> History()
    {
        var model = await addDropService.GetHistoryAsync(GetUserId());
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int sectionId)
    {
        var result = await addDropService.AddCourseAsync(GetUserId(), sectionId);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Drop(DropCourseInputModel input)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "A drop reason is required before removing a course.";
            return RedirectToAction(nameof(Index));
        }

        var result = await addDropService.DropCourseAsync(GetUserId(), input.EnrollmentId, input.Remarks);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? throw new InvalidOperationException("The current user is not authenticated.");
    }
}
