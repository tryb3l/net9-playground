using System;
using WebApplication1.Interfaces;
using WebApplication1.Models;
using WebApplication1.ViewModels;

namespace WebApplication1.Services;

public class PostService : IPostService
{
    private readonly IPostRepository _postRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ITagRepository _tagRepository;

    public PostService(IPostRepository postRepository, ICategoryRepository categoryRepository, ITagRepository tagRepository)
    {
        _postRepository = postRepository;
        _categoryRepository = categoryRepository;
        _tagRepository = tagRepository;
    }

    public async Task<BlogIndexViewModel> GetBlogIndexViewModelAsync(int page, string? category, string? tag)
    {
        int pageSize = 5;
        int skip = (page - 1) * pageSize;

        var posts = await _postRepository.GetPublishedPostsAsync(skip, pageSize);
        var totalPosts = await _postRepository.CountPublishedPostsAsync();

        var categories = await _categoryRepository.GetAllAsync();

        var tags = await _tagRepository.GetPopularTagsAsync(10);

        return new BlogIndexViewModel
        {
            Posts = posts.ToList(),
            Categories = categories.ToList(),
            Tags = tags.ToList(),
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(totalPosts / (double)pageSize),
            CurrentCategory = category,
            CurrentTag = tag
        };
    }

    public async Task<Post?> GetPostByIdAsync(int id)
    {
        return await _postRepository.GetPostWithDetailsAsync(id);
    }

}
