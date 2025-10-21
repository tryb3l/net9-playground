using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using WebApp.Areas.Admin.Controllers;
using WebApp.Areas.Admin.ViewModels.Post;
using WebApp.Interfaces;
using WebApp.Models;
using Shouldly;
using Xunit;

namespace WebApp.UnitTests.Controllers;

public class PostControllerTests
{
    private readonly Mock<IPostService> _mockPostService;
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly Mock<IActivityLogService> _mockActivityLogService;
    private readonly PostController _controller;

    public PostControllerTests()
    {
        _mockPostService = new Mock<IPostService>();
        _mockActivityLogService = new Mock<IActivityLogService>();

        var userStoreMock = new Mock<IUserStore<User>>();
        _mockUserManager = new Mock<UserManager<User>>(userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _controller = new PostController(
            _mockPostService.Object,
            Mock.Of<ICategoryService>(),
            Mock.Of<ITagService>(),
            _mockUserManager.Object,
            Mock.Of<ILogger<PostController>>(),
            _mockActivityLogService.Object
        )
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };
    }

    [Fact]
    public async Task Create_Post_WhenModelStateIsValid_ShouldCallServiceAndRedirect()
    {
        // Arrange
        var viewModel = new CreatePostViewModel { Title = "New Test Post", Content = "Test Content" };
        var user = new User { Id = "test-user-id", UserName = "testuser" };
        var createdPost = new Post { Id = 1, Title = viewModel.Title };

        _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        _mockPostService.Setup(s => s.CreatePostAsync(viewModel, user.Id)).ReturnsAsync(createdPost);

        // Act
        var result = await _controller.Create(viewModel);

        // Assert
        _mockPostService.Verify(s => s.CreatePostAsync(viewModel, user.Id), Times.Once);
        _mockActivityLogService.Verify(s => s.LogActivityAsync(user.Id, "Created", "Post", It.IsAny<string>()), Times.Once);

        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe("Index");
        _controller.TempData["SuccessMessage"].ShouldNotBeNull();
    }
}