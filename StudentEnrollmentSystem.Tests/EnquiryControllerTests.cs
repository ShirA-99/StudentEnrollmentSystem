using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentEnrollmentSystem.Controllers;
using StudentEnrollmentSystem.Data;
using StudentEnrollmentSystem.Models;

namespace StudentEnrollmentSystem.Tests;

public class EnquiryControllerTests
{
    [Fact]
    public async Task ContactUs_WhenModelIsValid_PersistsMessageAndRedirectsToSuccess()
    {
        await using var context = CreateContext();
        var controller = new EnquiryController(context);
        var model = new ContactUsMessage
        {
            Name = "Aisyah",
            Email = "aisyah@example.com",
            Subject = "Need enrollment help",
            Message = "I am unable to access the add/drop page."
        };

        var result = await controller.ContactUs(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(EnquiryController.ContactUsSuccess), redirect.ActionName);

        var savedMessage = await context.ContactUsMessages.SingleAsync();
        Assert.Equal(model.Name, savedMessage.Name);
        Assert.Equal(model.Email, savedMessage.Email);
        Assert.Equal(model.Subject, savedMessage.Subject);
        Assert.Equal(model.Message, savedMessage.Message);
        Assert.NotEqual(default, savedMessage.SubmittedAt);
    }

    [Fact]
    public async Task ContactUs_WhenModelIsInvalid_ReturnsViewWithoutPersisting()
    {
        await using var context = CreateContext();
        var controller = new EnquiryController(context);
        controller.ModelState.AddModelError(nameof(ContactUsMessage.Email), "Email is required.");

        var result = await controller.ContactUs(new ContactUsMessage());

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<ContactUsMessage>(view.Model);
        Assert.Empty(context.ContactUsMessages);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
