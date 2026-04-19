using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentEnrollmentSystem.Data;
using StudentEnrollmentSystem.Models;
using StudentEnrollmentSystem.Services;
using StudentEnrollmentSystem.ViewModels;

namespace StudentEnrollmentSystem.Controllers;

[Authorize]
public class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ApplicationDbContext context,
    ProfileProtectionService profileProtectionService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return Challenge();
        }

        var model = await BuildProfilePageViewModelAsync(user);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile([Bind(Prefix = "Profile")] ProfileInfoInputModel input)
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return Challenge();
        }

        if (await EmailBelongsToAnotherUserAsync(user.Id, input.Email))
        {
            ModelState.AddModelError(nameof(input.Email), "That email address is already in use.");
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildProfilePageViewModelAsync(user, profileInput: input, activeSection: "profile");
            return View("Profile", invalidModel);
        }

        user.DisplayName = input.FullName.Trim();
        user.Email = input.Email.Trim();
        user.UserName = input.Email.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(input.PhoneNumber) ? null : input.PhoneNumber.Trim();
        user.Address = string.IsNullOrWhiteSpace(input.Address) ? null : input.Address.Trim();
        user.Postcode = string.IsNullOrWhiteSpace(input.Postcode) ? null : input.Postcode.Trim();
        user.City = string.IsNullOrWhiteSpace(input.City) ? null : input.City.Trim();
        user.State = string.IsNullOrWhiteSpace(input.State) ? null : input.State.Trim();
        user.Country = string.IsNullOrWhiteSpace(input.Country) ? null : input.Country.Trim();

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            AddErrors(updateResult);
            var failedModel = await BuildProfilePageViewModelAsync(user, profileInput: input, activeSection: "profile");
            return View("Profile", failedModel);
        }

        var studentProfile = await context.StudentProfiles
            .SingleOrDefaultAsync(profile => profile.ApplicationUserId == user.Id);

        if (studentProfile is not null)
        {
            studentProfile.FullName = input.FullName.Trim();
            studentProfile.Email = input.Email.Trim();
            await context.SaveChangesAsync();
        }

        await signInManager.RefreshSignInAsync(user);
        TempData["SuccessMessage"] = "Your profile details were updated successfully.";

        return RedirectToAction(nameof(Profile), new { section = "profile" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateBankDetails([Bind(Prefix = "BankDetails")] BankDetailsInputModel input)
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildProfilePageViewModelAsync(user, bankDetailsInput: input, activeSection: "bank");
            return View("Profile", invalidModel);
        }

        user.BankName = input.BankName.Trim();
        user.EncryptedBankAccountNumber = profileProtectionService.Protect(input.AccountNumber);
        user.EncryptedBankAccountHolderName = profileProtectionService.Protect(input.AccountHolderName);

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            AddErrors(updateResult);
            var failedModel = await BuildProfilePageViewModelAsync(user, bankDetailsInput: input, activeSection: "bank");
            return View("Profile", failedModel);
        }

        TempData["SuccessMessage"] = "Your bank details were updated successfully.";
        return RedirectToAction(nameof(Profile), new { section = "bank" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword([Bind(Prefix = "Password")] ChangePasswordInputModel input)
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildProfilePageViewModelAsync(user, passwordInput: input, activeSection: "password");
            return View("Profile", invalidModel);
        }

        var changeResult = await userManager.ChangePasswordAsync(user, input.CurrentPassword, input.NewPassword);
        if (!changeResult.Succeeded)
        {
            AddErrors(changeResult);
            var failedModel = await BuildProfilePageViewModelAsync(user, passwordInput: input, activeSection: "password");
            return View("Profile", failedModel);
        }

        await signInManager.RefreshSignInAsync(user);
        TempData["SuccessMessage"] = "Your password was changed successfully.";

        return RedirectToAction(nameof(Profile), new { section = "password" });
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
        => await userManager.GetUserAsync(User);

    private async Task<bool> EmailBelongsToAnotherUserAsync(string userId, string? email)
    {
        var normalizedEmail = email?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return false;
        }

        var existingUser = await userManager.FindByEmailAsync(normalizedEmail);
        return existingUser is not null && existingUser.Id != userId;
    }

    private async Task<UserProfilePageViewModel> BuildProfilePageViewModelAsync(
        ApplicationUser user,
        ProfileInfoInputModel? profileInput = null,
        BankDetailsInputModel? bankDetailsInput = null,
        ChangePasswordInputModel? passwordInput = null,
        string? activeSection = null)
    {
        var studentProfile = await context.StudentProfiles
            .AsNoTracking()
            .Include(profile => profile.CurrentSemester)
            .SingleOrDefaultAsync(profile => profile.ApplicationUserId == user.Id);

        var decryptedAccountNumber = profileProtectionService.Unprotect(user.EncryptedBankAccountNumber);
        var decryptedAccountHolderName = profileProtectionService.Unprotect(user.EncryptedBankAccountHolderName);
        var resolvedSection = activeSection;

        if (string.IsNullOrWhiteSpace(resolvedSection))
        {
            resolvedSection = Request.Query["section"].ToString();
        }

        if (string.IsNullOrWhiteSpace(resolvedSection))
        {
            resolvedSection = "profile";
        }

        return new UserProfilePageViewModel
        {
            UserId = user.Id,
            DisplayName = user.DisplayName,
            EmailAddress = user.Email ?? string.Empty,
            StudentNumber = studentProfile?.StudentNumber,
            ProgramName = studentProfile?.ProgramName,
            IntakeLabel = studentProfile?.IntakeLabel,
            CurrentSemesterName = studentProfile?.CurrentSemester?.Name,
            BankAccountMask = ProfileProtectionService.MaskAccountNumber(decryptedAccountNumber),
            ActiveSection = resolvedSection,
            Profile = profileInput ?? new ProfileInfoInputModel
            {
                FullName = user.DisplayName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                Postcode = user.Postcode,
                City = user.City,
                State = user.State,
                Country = user.Country
            },
            BankDetails = bankDetailsInput ?? new BankDetailsInputModel
            {
                BankName = user.BankName ?? string.Empty,
                AccountNumber = decryptedAccountNumber ?? string.Empty,
                AccountHolderName = decryptedAccountHolderName ?? string.Empty
            },
            Password = passwordInput ?? new ChangePasswordInputModel()
        };
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}
