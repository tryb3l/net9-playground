using System;
using System.Collections.Generic;
using WebApp.Models;

namespace WebApp.UnitTests.TestUtils.Builders;

public class PostBuilder
{
    private int _id = 1;
    private string _title = "Test Post";
    private string _slug = "test-post";
    private string _content = "Test content";
    private string _authorId = "test-author-id";
    private bool _isPublished = true;
    private DateTime _createdAt = DateTime.UtcNow;
    private DateTime? _publishedDate = DateTime.UtcNow;
    private int? _categoryId;
    private List<PostTag> _postTags = [];

    public PostBuilder WithId(int id)
    {
        _id = id;
        return this;
    }

    public PostBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public PostBuilder WithSlug(string slug)
    {
        _slug = slug;
        return this;
    }

    public PostBuilder WithContent(string content)
    {
        _content = content;
        return this;
    }

    public PostBuilder WithAuthor(string authorId)
    {
        _authorId = authorId;
        return this;
    }

    public PostBuilder WithIsPublished(bool isPublished)
    {
        _isPublished = isPublished;
        return this;
    }

    public PostBuilder WithCreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public PostBuilder WithPublishedDate(DateTime? publishedDate)
    {
        _publishedDate = publishedDate;
        return this;
    }

    public PostBuilder WithCategoryId(int? categoryId)
    {
        _categoryId = categoryId;
        return this;
    }

    public PostBuilder WithPostTags(List<PostTag> postTags)
    {
        _postTags = postTags;
        return this;
    }

    public Post Build()
    {
        return new Post
        {
            Id = _id,
            Title = _title,
            Slug = _slug,
            Content = _content,
            AuthorId = _authorId,
            IsPublished = _isPublished,
            CreatedAt = _createdAt,
            PublishedDate = _publishedDate,
            CategoryId = _categoryId,
            PostTags = _postTags
        };
    }
}