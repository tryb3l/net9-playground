using System.Collections.Generic;
using System.Linq;
using WebApp.ViewModels;

namespace WebApp.UnitTests.TestUtils.Builders;

public sealed class BlogIndexViewModelBuilder
{
    private List<PostCardViewModel> _posts = [];
    private List<CategoryViewModel> _categories = [];
    private List<TagViewModel> _tags = [];
    private int _currentPage = 1;
    private int _totalPages = 1;
    private string? _currentCategory;
    private string? _currentTag;

    public BlogIndexViewModelBuilder WithPosts(params PostCardViewModel[] posts)
    {
        _posts = posts.ToList();
        return this;
    }

    public BlogIndexViewModelBuilder WithCategories(params CategoryViewModel[] categories)
    {
        _categories = categories.ToList();
        return this;
    }

    public BlogIndexViewModelBuilder WithTags(params TagViewModel[] tags)
    {
        _tags = tags.ToList();
        return this;
    }

    public BlogIndexViewModelBuilder WithCurrentPage(int page)
    {
        _currentPage = page;
        return this;
    }

    public BlogIndexViewModelBuilder WithTotalPages(int totalPages)
    {
        _totalPages = totalPages;
        return this;
    }

    public BlogIndexViewModelBuilder WithCurrentCategory(string? category)
    {
        _currentCategory = category;
        return this;
    }

    public BlogIndexViewModelBuilder WithCurrentTag(string? tag)
    {
        _currentTag = tag;
        return this;
    }

    public BlogIndexViewModel Build() => new()
    {
        Posts = _posts,
        Categories = _categories,
        Tags = _tags,
        CurrentPage = _currentPage,
        TotalPages = _totalPages,
        CurrentCategory = _currentCategory,
        CurrentTag = _currentTag,
        Category = _currentCategory,
        Tag = _currentTag
    };
}