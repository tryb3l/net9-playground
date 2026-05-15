using System;
using AutoMapper;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using WebApp.Areas.Admin.Mapping;
using WebApp.Areas.Admin.ViewModels.Post;
using WebApp.Models;
using WebApp.ViewModels;
using Xunit;
using CategoryViewModel = WebApp.Areas.Admin.ViewModels.Category.CategoryViewModel;

namespace WebApp.UnitTests.Mapping;

public class AutoMapperConfigurationTests
{
    private readonly IMapper _mapper;

    public AutoMapperConfigurationTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AdminMappingProfile>();
        }, NullLoggerFactory.Instance);

        _mapper = config.CreateMapper();
    }

    [Fact]
    public void Configuration_ShouldBeValid()
    {
        // This will throw if any mappings are misconfigured
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AdminMappingProfile>();
        }, NullLoggerFactory.Instance);

        config.AssertConfigurationIsValid();
    }

    [Fact]
    public void Tag_To_SelectListItem_ShouldMap()
    {
        // Arrange
        var tag = new Tag { Id = 1, Name = "Test Tag" };

        // Act
        var result = _mapper.Map<SelectListItem>(tag);

        // Assert
        result.ShouldNotBeNull();
        result.Value.ShouldBe("1");
        result.Text.ShouldBe("Test Tag");
    }

    [Fact]
    public void Category_To_PublicCategoryViewModel_ShouldMap()
    {
        // Arrange
        var category = new Category 
        { 
            Id = 1, 
            Name = "Tech", 
            Slug = "tech",
            Posts = [new Post { Id = 1, Title = "Test" }]
        };

        // Act
        var result = _mapper.Map<CategoryViewModel>(category);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Tech");
        result.PostCount.ShouldBe(1);
    }

    [Fact]
    public void Category_To_AdminCategoryViewModel_ShouldMap()
    {
        // Arrange
        var category = new Category 
        { 
            Id = 1, 
            Name = "Tech", 
            Slug = "tech",
            Posts = null!
        };

        // Act
        var result = _mapper.Map<CategoryViewModel>(category);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Tech");
        result.PostCount.ShouldBe(0);
    }

    [Fact]
    public void Tag_To_TagViewModel_WithNullPostTags_ShouldMapToZero()
    {
        // Arrange
        var tag = new Tag { Id = 1, Name = "Test", PostTags = null! };

        // Act
        var result = _mapper.Map<TagViewModel>(tag);

        // Assert
        result.PostCount.ShouldBe(0);
    }

    [Fact]
    public void EditPostViewModel_To_Post_ShouldNotOverwriteSystemFields()
    {
        // Arrange
        var existingPost = new Post
        {
            Id = 1,
            Title = "Original",
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            PublishedDate = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            AuthorId = "original-author",
            Slug = "original-slug"
        };

        var editModel = new EditPostViewModel
        {
            Id = 1,
            Title = "Updated Title",
            Content = "Updated content"
        };

        // Act
        _mapper.Map(editModel, existingPost);

        // Assert - System fields should be preserved
        existingPost.CreatedAt.ShouldBe(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        existingPost.PublishedDate.ShouldBe(new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc));
        existingPost.AuthorId.ShouldBe("original-author");
        existingPost.Slug.ShouldBe("original-slug");
        
        // But title and content should update
        existingPost.Title.ShouldBe("Updated Title");
        existingPost.Content.ShouldBe("Updated content");
    }
}