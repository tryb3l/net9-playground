using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using WebApp.Areas.Admin.Controllers;
using WebApp.Interfaces;
using WebApp.Models;
using WebApp.UnitTests.TestUtils.Factories;

namespace WebApp.UnitTests.TestUtils.Builders;

public class PostControllerBuilder
{
    // It expose mocks to tests so they can setup/verify
    public Mock<IPostService> PostService { get; } = new();
    private Mock<ICategoryService> CategoryService { get; } = new();
    private Mock<ITagService> TagService { get; } = new();
    public Mock<IActivityLogService> ActivityLogService { get; } = new();
    private Mock<UserManager<User>> UserManager { get; }
    private Mock<ILogger<PostController>> Logger { get; } = new();

    private ClaimsPrincipal? _userPrincipal;

    public PostControllerBuilder()
    {
        UserManager = MockUserManagerFactory.Create();
    }

    public PostControllerBuilder WithAdminUser(string userId = "user-1", string userName = "admin")
    {
        var user = new User { Id = userId, UserName = userName };
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, userName)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _userPrincipal = new ClaimsPrincipal(identity);

        UserManager.Setup(um => um.GetUserAsync(_userPrincipal)).ReturnsAsync(user);
        return this;
    }

    public PostController Build()
    {
        var controller = new PostController(
            PostService.Object,
            CategoryService.Object,
            TagService.Object,
            UserManager.Object,
            Logger.Object,
            ActivityLogService.Object
        )
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    // If WithAdminUser was called, set the user; otherwise leave default
                    User = _userPrincipal ?? new ClaimsPrincipal()
                }
            },
            // Essential for tests that check Redirects or View results relying on TempData
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };

        return controller;
    }
}