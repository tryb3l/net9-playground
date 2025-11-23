using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shouldly;
using WebApp.Areas.Admin.ViewModels.Post;
using WebApp.Controllers;
using WebApp.Interfaces;
using WebApp.UnitTests.TestUtils;
using WebApp.UnitTests.TestUtils.Builders;
using Xunit;

namespace WebApp.UnitTests.Controllers;

public class BlogControllerTests : ControllerTestBase
{
    private readonly Mock<IPostService> _mockPostService = new();
    private readonly BlogController _controller;

    public BlogControllerTests()
    {
        _controller = new BlogController(_mockPostService.Object);
        SetupControllerContext(_controller);
    }

    [Fact]
    public async Task Index_ShouldCallServiceWithParameters_AndReturnView()
    {
        // Arrange
        const int page = 2;
        const string category = "Tech";
        const string tag = "CSharp";
        var expectedVm = new BlogIndexViewModelBuilder()
            .WithCurrentPage(page)
            .WithCurrentCategory(category)
            .WithCurrentTag(tag)
            .Build();

        _mockPostService.Setup(s => s.GetBlogIndexViewModelAsync(page, category, tag))
            .ReturnsAsync(expectedVm);

        // Act
        var result = await _controller.Index(page, category, tag);

        // Assert
        _mockPostService.Verify(s => s.GetBlogIndexViewModelAsync(page, category, tag), Times.Once);
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBe(expectedVm);
    }

    [Fact]
    public async Task Post_ShouldReturnView_WhenPostExists()
    {
        // Arrange
        const string slug = "test-post";
        var expectedVm = new PostViewModel { Title = "Test Post"};

        _mockPostService.Setup(s => s.GetPostViewModelBySlugAsync(slug))
            .ReturnsAsync(expectedVm);

        // Act
        var result = await _controller.Post(slug);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBe(expectedVm);
    }

    [Fact]
    public async Task Post_ShouldReturnNotFound_WhenPostDoesNotExist()
    {
        // Arrange
        const string slug = "missing-post";
        _mockPostService.Setup(s => s.GetPostViewModelBySlugAsync(slug))
            .ReturnsAsync((PostViewModel?)null);

        // Act
        var result = await _controller.Post(slug);

        // Assert
        result.ShouldBeOfType<NotFoundResult>();
    }
}