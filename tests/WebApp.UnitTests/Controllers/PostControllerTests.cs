using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shouldly;
using WebApp.Areas.Admin.ViewModels.Post;
using WebApp.UnitTests.TestUtils.Builders;
using Xunit;

namespace WebApp.UnitTests.Controllers;

public class PostControllerTests
{
    private const string TestUserId = "user-1";

    [Fact]
    public async Task Create_ValidModel_RedirectsToEdit()
    {
        // Arrange
        var builder = new PostControllerBuilder().WithAdminUser(TestUserId);
        
        var viewModel = new CreatePostViewModel { Title = "New Post" };
        var createdPost = new PostBuilder().WithId(123).WithTitle("New Post").Build();
        
        builder.PostService.Setup(s => s.CreatePostAsync(viewModel, TestUserId)).ReturnsAsync(createdPost);
        
        var sut = builder.Build();

        // Act
        var result = await sut.Create(viewModel);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe("Edit");
        redirect.RouteValues?["id"].ShouldBe(123);
    }

    [Fact]
    public async Task Create_ValidModel_LogsActivity()
    {
        // Arrange
        var builder = new PostControllerBuilder().WithAdminUser(TestUserId);
        
        var viewModel = new CreatePostViewModel { Title = "New Post" };
        var createdPost = new PostBuilder().WithId(123).WithTitle("New Post").Build();
        
        builder.PostService.Setup(s => s.CreatePostAsync(viewModel, TestUserId)).ReturnsAsync(createdPost);
        
        var sut = builder.Build();

        // Act
        await sut.Create(viewModel);

        // Assert
        builder.ActivityLogService.Verify(l => l.LogActivityAsync(TestUserId, "Created", "Post", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SoftDelete_ExistingPost_ReturnsSuccessJson()
    {
        // Arrange
        var builder = new PostControllerBuilder().WithAdminUser(TestUserId);
        
        var postId = 1;
        var post = new PostBuilder().WithId(postId).WithTitle("Test Post").Build();
        builder.PostService.Setup(s => s.GetPostByIdAsync(postId, true, false)).ReturnsAsync(post);

        var sut = builder.Build();

        // Act
        var result = await sut.SoftDelete(postId);

        // Assert
        var jsonResult = result.ShouldBeOfType<JsonResult>();
        var value = jsonResult.Value;
        var success = value?.GetType().GetProperty("success")?.GetValue(value);
        success.ShouldBe(true);
    }
}