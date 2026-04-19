using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StudentEnrollmentSystem.Data;
using StudentEnrollmentSystem.Models;
using StudentEnrollmentSystem.ViewModels;

namespace StudentEnrollmentSystem.Areas.Identity.Pages.Account;

public class LoginModel(
    SignInManager<ApplicationUser> signInManager,
    ILogger<LoginModel> logger) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public IReadOnlyList<DemoLoginViewModel> DemoAccounts { get; } =
    [
        new DemoLoginViewModel { DisplayName = "Alice Tan", Email = SeedDataDefaults.DemoEmails[0] },
        new DemoLoginViewModel { DisplayName = "Bob Kumar", Email = SeedDataDefaults.DemoEmails[1] },
        new DemoLoginViewModel { DisplayName = "Chloe Lim", Email = SeedDataDefaults.DemoEmails[2] },
        new DemoLoginViewModel { DisplayName = "Daniel Wong", Email = SeedDataDefaults.DemoEmails[3] },
        new DemoLoginViewModel { DisplayName = "Farah Hassan", Email = SeedDataDefaults.DemoEmails[4] }
    ];

    public async Task OnGetAsync(string? returnUrl = null, string? demoEmail = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");

        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        if (!string.IsNullOrWhiteSpace(demoEmail))
        {
            Input.Email = demoEmail;
        }
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await signInManager.PasswordSignInAsync(
            Input.Email,
            Input.Password,
            Input.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            logger.LogInformation("User logged in.");
            return LocalRedirect(ReturnUrl);
        }

        if (result.IsLockedOut)
        {
            logger.LogWarning("User account locked out.");
            ModelState.AddModelError(string.Empty, "This account is temporarily locked. Please try again later.");
            return Page();
        }

        ModelState.AddModelError(
            string.Empty,
            "We couldn't sign you in with those details. Check your email and password, or use one of the demo accounts below.");
        return Page();
    }

    public sealed class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Student email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Keep me signed in on this device")]
        public bool RememberMe { get; set; }
    }
}
