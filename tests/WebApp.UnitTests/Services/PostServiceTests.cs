using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Moq;
using Shouldly;
using WebApp.Areas.Admin.ViewModels.Post;
using WebApp.Interfaces;
using WebApp.Models;
using WebApp.Services;
using WebApp.UnitTests.TestUtils.Builders;
using Xunit;

namespace WebApp.UnitTests.Services;

public class PostServiceTests
{
    private readonly Mock<IPostRepository> _mockPostRepository = new();
    private readonly Mock<IMapper> _mockMapper = new();
    private readonly Mock<ICategoryRepository> _mockCategoryRepository = new();
    private readonly Mock<ITagRepository> _mockTagRepository = new();
    private readonly Mock<LinkGenerator> _mockLinkGenerator = new();
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor = new();
    private readonly Mock<ITagService> _mockTagService = new();

    private readonly PostService _postService;

    public PostServiceTests()
    {
        _postService = new PostService(
            _mockPostRepository.Object,
            _mockCategoryRepository.Object,
            _mockTagRepository.Object,
            _mockLinkGenerator.Object,
            _mockHttpContextAccessor.Object,
            _mockMapper.Object,
            _mockTagService.Object
        );
    }

    [Fact]
    public async Task CreatePostAsync_ShouldCreateAndSavePost()
    {
        // Arrange
        var viewModel = new CreatePostViewModel { Title = "Test Post 123", Content = "Content 123" };
        const string userId = "user12345";

        var expectedPost = new PostBuilder()
            .WithTitle(viewModel.Title)
            .WithAuthor(userId)
            .Build();

        _mockMapper.Setup(m => m.Map<Post>(viewModel)).Returns(expectedPost);
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

    [Theory]
    [InlineData("Old Title", "Old Title", "old-title")]
    [InlineData("Old Title", "New Title", "new-title")]
    public async Task UpdatePostAsync_ShouldUpdateFields_AndRegenerateSlug_WhenTitleChanges(
        string oldTitle, string newTitle, string expectedSlug)
    {
        // Arrange
        const int postId = 1;
        var existingPost = new PostBuilder()
            .WithId(postId)
            .WithTitle(oldTitle)
            .WithSlug(oldTitle.ToLower().Replace(" ", "-"))
            .WithPostTags([
                new PostTag { PostId = postId, TagId = 1 },
                new PostTag { PostId = postId, TagId = 2 }
            ])
            .Build();

        var updateModel = new EditPostViewModel
        {
            Id = postId,
            Title = newTitle,
            Content = "New Content",
            PublishNow = true,
            SelectedTagIds = [10, 20]
        };

        Post? updatedPost = null;

        _mockPostRepository.Setup(r => r.GetPostWithDetailsAsync(postId))
            .ReturnsAsync(existingPost);

        _mockPostRepository.Setup(r => r.SlugExistsAsync(It.IsAny<string>(), postId))
            .ReturnsAsync(false);

        _mockMapper.Setup(m => m.Map(updateModel, existingPost))
            .Returns((EditPostViewModel source, Post destination) =>
            {
                destination.Title = source.Title;
                destination.Content = source.Content;
                return destination;
            });

        _mockPostRepository.As<IRepository<Post>>()
            .Setup(r => r.UpdateAsync(It.IsAny<Post>()))
            .Callback<Post>(p => updatedPost = p)
            .Returns(Task.CompletedTask);

        _mockPostRepository.As<IRepository<Post>>()
            .Setup(r => r.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _postService.UpdatePostAsync(postId, updateModel);

        // Assert
        updatedPost.ShouldNotBeNull();
        updatedPost!.Title.ShouldBe(newTitle);
        updatedPost.Slug.ShouldBe(expectedSlug);
        updatedPost.Content.ShouldBe(updateModel.Content);
        updatedPost.IsPublished.ShouldBeTrue();
    }
}