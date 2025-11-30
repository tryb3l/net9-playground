using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using WebApp.Areas.Admin.Controllers;
using WebApp.Areas.Admin.ViewModels.Post;
using WebApp.Interfaces;
using WebApp.Models;
using WebApp.UnitTests.TestUtils;
using WebApp.UnitTests.TestUtils.Builders;
using WebApp.UnitTests.TestUtils.Factories;
using Xunit;

namespace WebApp.UnitTests.Controllers;

public class PostControllerTests : ControllerTestBase
{
    private readonly Mock<IPostService> _mockPostService = new();
    private readonly Mock<ICategoryService> _mockCategoryService = new();
    private readonly Mock<ITagService> _mockTagService = new();
    private readonly Mock<UserManager<User>> _mockUserManager = MockUserManagerFactory.Create();
    private readonly Mock<IActivityLogService> _mockActivityLogService = new();
    private readonly PostController _controller;

    public PostControllerTests()
    {
        _controller = new PostController(
            _mockPostService.Object,
            _mockCategoryService.Object,
            _mockTagService.Object,
            _mockUserManager.Object,
            Mock.Of<ILogger<PostController>>(),
            _mockActivityLogService.Object
        );

        SetupControllerContext(_controller);
    }

    [Fact]
    public async Task Create_Post_WhenModelStateIsValid_ShouldCallServiceAndRedirect()
    {
        // Arrange
        var viewModel = new CreatePostViewModel { Title = "New Test Post", Content = "Test Content" };
        var user = new User { Id = "test-user", UserName = "testuser" };

        var createdPost = new PostBuilder()
            .WithTitle(viewModel.Title)
            .WithAuthor(user.Id)
            .Build();

        _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        _mockPostService.Setup(s => s.CreatePostAsync(viewModel, user.Id)).ReturnsAsync(createdPost);

        // Act
        var result = await _controller.Create(viewModel);

        // Assert
        _mockPostService.Verify(s => s.CreatePostAsync(viewModel, user.Id), Times.Once);

        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe("Index");
        _controller.TempData["SuccessMessage"].ShouldNotBeNull();
    }

    [Fact]
    public async Task Create_Post_WhenModelStateIsInvalid_ShouldReturnViewWithErrors()
    {
        // Arrange
        var viewModel = new CreatePostViewModel { Title = "", Content = "Test Content" };
        _controller.ModelState.AddModelError("Title", "Title is required");
        
        _mockTagService.Setup(s => s.GetAvailableTagsAsync()).ReturnsAsync([]);
        _mockCategoryService.Setup(s => s.GetAvailableCategoriesAsync()).ReturnsAsync([]);

        // Act
        var result = await _controller.Create(viewModel);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBe(viewModel);
        _mockPostService.Verify(s => s.CreatePostAsync(It.IsAny<CreatePostViewModel>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Create_Post_WhenUserNotFound_ShouldReturnUnauthorized()
    {
        // Arrange
        var viewModel = new CreatePostViewModel { Title = "Test Post", Content = "Content" };
        _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _controller.Create(viewModel);

        // Assert
        result.ShouldBeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Create_Post_WhenServiceThrows_ShouldReturnViewWithErrorMessage()
    {
        // Arrange
        var viewModel = new CreatePostViewModel { Title = "Test Post", Content = "Content" };
        var user = new User { Id = "test-user", UserName = "testuser" };

        _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        _mockPostService.Setup(s => s.CreatePostAsync(viewModel, user.Id))
            .ThrowsAsync(new Exception("Database error"));
        
        _mockTagService.Setup(s => s.GetAvailableTagsAsync()).ReturnsAsync([]);
        _mockCategoryService.Setup(s => s.GetAvailableCategoriesAsync()).ReturnsAsync([]);

        // Act
        var result = await _controller.Create(viewModel);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ShouldNotBeNull();
        _controller.TempData["ErrorMessage"].ShouldNotBeNull();
        _controller.TempData["ErrorMessage"]!.ToString()!.ShouldContain("unexpected error");
    }
}