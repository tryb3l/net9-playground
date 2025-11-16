using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Moq;
using WebApp.Areas.Admin.ViewModels.Post;
using WebApp.Interfaces;
using WebApp.Models;
using WebApp.Services;
using Shouldly;
using Xunit;

namespace WebApp.UnitTests.Services;

public class PostServiceTests
{
    private readonly Mock<IPostRepository> _mockPostRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly PostService _postService;

    public PostServiceTests()
    {
        _mockPostRepository = new Mock<IPostRepository>();
        _mockMapper = new Mock<IMapper>();

        _postService = new PostService(
            _mockPostRepository.Object,
            Mock.Of<ICategoryRepository>(),
            Mock.Of<ITagRepository>(),
            Mock.Of<LinkGenerator>(),
            Mock.Of<IHttpContextAccessor>(),
            _mockMapper.Object,
            Mock.Of<ITagService>()
        );
    }

    [Fact]
    public async Task CreatePostAsync_ShouldCreateAndSavePost()
    {
        // Arrange
        var viewModel = new CreatePostViewModel { Title = "New Post 123", Content = "Content123" };
        const string userId = "user12345";
        var post = new Post { Title = viewModel.Title, Content = viewModel.Content };

        _mockMapper.Setup(m => m.Map<Post>(viewModel)).Returns(post);
        _mockPostRepository.Setup(r => r.SlugExistsAsync(It.IsAny<string>(), null)).ReturnsAsync(false);

        _mockPostRepository.As<IRepository<Post>>()
            .Setup(r => r.AddAsync(It.IsAny<Post>()))
            .Returns(Task.CompletedTask);

        _mockPostRepository.As<IRepository<Post>>()
            .Setup(r => r.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _postService.CreatePostAsync(viewModel, userId);

        // Assert
        result.ShouldNotBeNull();
        result.AuthorId.ShouldBe(userId);
        result.Slug.ShouldNotBeNullOrEmpty();

        _mockPostRepository.As<IRepository<Post>>()
            .Verify(r => r.AddAsync(It.IsAny<Post>()), Times.Once);

        _mockPostRepository.As<IRepository<Post>>()
            .Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}