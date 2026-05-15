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

    [Fact]
    public async Task CreatePostAsync_ShouldGenerateSlug_FromTitle()
    {
        // Arrange
        var viewModel = new CreatePostViewModel { Title = "Hello World" };
        const string userId = "user-1";

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
        result.Slug.ShouldBe("hello-world");
    }

    [Fact]
    public async Task CreatePostAsync_ShouldAppendCounter_WhenSlugCollides()
    {
        // Arrange
        var viewModel = new CreatePostViewModel { Title = "Hello World" };
        const string userId = "user-1";

        _mockPostRepository.SetupSequence(r => r.SlugExistsAsync(It.IsAny<string>(), null))
            .ReturnsAsync(true)   // "hello-world" already taken
            .ReturnsAsync(false); // "hello-world-1" is free

        _mockPostRepository.As<IRepository<Post>>()
            .Setup(r => r.AddAsync(It.IsAny<Post>()))
            .Returns(Task.CompletedTask);
        _mockPostRepository.As<IRepository<Post>>()
            .Setup(r => r.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _postService.CreatePostAsync(viewModel, userId);

        // Assert
        result.Slug.ShouldBe("hello-world-1");
    }

    [Fact]
    public async Task UpdatePostAsync_ShouldNotRegenerateSlug_WhenPostIsPublished_AndTitleChanges()
    {
        // Arrange
        const int postId = 1;
        const string originalSlug = "original-title";
        var existingPost = new PostBuilder()
            .WithId(postId)
            .WithTitle("Original Title")
            .WithSlug(originalSlug)
            .WithIsPublished(true)
            .WithPostTags([])
            .Build();

        var updateModel = new EditPostViewModel
        {
            Id = postId,
            Title = "Changed Title",
            Content = "Content",
            PublishNow = true,
            SelectedTagIds = []
        };

        _mockPostRepository.Setup(r => r.GetPostWithDetailsAsync(postId)).ReturnsAsync(existingPost);
        _mockMapper.Setup(m => m.Map(updateModel, existingPost))
            .Returns((EditPostViewModel src, Post dest) =>
            {
                dest.Title = src.Title;
                dest.Content = src.Content;
                return dest;
            });
        _mockPostRepository.As<IRepository<Post>>()
            .Setup(r => r.UpdateAsync(It.IsAny<Post>()))
            .Returns(Task.CompletedTask);
        _mockPostRepository.As<IRepository<Post>>()
            .Setup(r => r.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _postService.UpdatePostAsync(postId, updateModel);

        // Assert
        existingPost.Slug.ShouldBe(originalSlug);
        _mockPostRepository.Verify(r => r.SlugExistsAsync(It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePostAsync_ShouldPreserveJsonFeaturedImage_WhenImageUrlMatchesStoredLargeUrl()
    {
        // Arrange
        const int postId = 1;
        const string storedJson = """{"large":"/uploads/large.jpg","thumbnail":"/uploads/thumb.jpg"}""";

        var existingPost = new PostBuilder()
            .WithId(postId)
            .WithIsPublished(false)
            .WithPostTags([])
            .Build();
        existingPost.FeaturedImageUrls = storedJson;

        var updateModel = new EditPostViewModel
        {
            Id = postId,
            Title = "Title",
            Content = "Content",
            PublishNow = false,
            FeaturedImageUrl = "/uploads/large.jpg", // same as stored large URL
            SelectedTagIds = []
        };

        _mockPostRepository.Setup(r => r.GetPostWithDetailsAsync(postId)).ReturnsAsync(existingPost);
        _mockMapper.Setup(m => m.Map(updateModel, existingPost))
            .Returns((EditPostViewModel src, Post dest) =>
            {
                dest.Title = src.Title;
                // FeaturedImageUrls intentionally NOT mapped (as per AdminMappingProfile Ignore())
                return dest;
            });
        _mockPostRepository.As<IRepository<Post>>()
            .Setup(r => r.UpdateAsync(It.IsAny<Post>()))
            .Returns(Task.CompletedTask);
        _mockPostRepository.As<IRepository<Post>>()
            .Setup(r => r.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _postService.UpdatePostAsync(postId, updateModel);

        // Assert — JSON blob must be preserved, not replaced by the plain URL
        existingPost.FeaturedImageUrls.ShouldBe(storedJson);
    }

    [Fact]
    public async Task UpdatePostAsync_ShouldUpdateFeaturedImageUrls_WhenImageUrlChanges()
    {
        // Arrange
        const int postId = 1;
        const string storedJson = """{"large":"/uploads/old-large.jpg","thumbnail":"/uploads/old-thumb.jpg"}""";
        const string newImageUrl = "/uploads/new-image.jpg";

        var existingPost = new PostBuilder()
            .WithId(postId)
            .WithIsPublished(false)
            .WithPostTags([])
            .Build();
        existingPost.FeaturedImageUrls = storedJson;

        var updateModel = new EditPostViewModel
        {
            Id = postId,
            Title = "Title",
            Content = "Content",
            PublishNow = false,
            FeaturedImageUrl = newImageUrl, // different from stored large URL
            SelectedTagIds = []
        };

        _mockPostRepository.Setup(r => r.GetPostWithDetailsAsync(postId)).ReturnsAsync(existingPost);
        _mockMapper.Setup(m => m.Map(updateModel, existingPost))
            .Returns((EditPostViewModel src, Post dest) =>
            {
                dest.Title = src.Title;
                return dest;
            });
        _mockPostRepository.As<IRepository<Post>>()
            .Setup(r => r.UpdateAsync(It.IsAny<Post>()))
            .Returns(Task.CompletedTask);
        _mockPostRepository.As<IRepository<Post>>()
            .Setup(r => r.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _postService.UpdatePostAsync(postId, updateModel);

        // Assert
        existingPost.FeaturedImageUrls.ShouldBe(newImageUrl);
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
            .WithIsPublished(false)
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