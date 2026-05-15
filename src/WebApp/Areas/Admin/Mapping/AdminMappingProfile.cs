using System.Text.Json;
using AutoMapper;
using Markdig;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApp.Areas.Admin.ViewModels.Category;
using WebApp.Areas.Admin.ViewModels.Dashboard;
using WebApp.Areas.Admin.ViewModels.Post;
using WebApp.Areas.Admin.ViewModels.Tag;
using WebApp.Models;
using WebApp.ViewModels;
using AdminCategoryViewModel = WebApp.Areas.Admin.ViewModels.Category.CategoryViewModel;
using TagViewModel = WebApp.ViewModels.TagViewModel;

namespace WebApp.Areas.Admin.Mapping;

public class AdminMappingProfile : Profile
{
    public AdminMappingProfile()
    {
        CreateMap<Post, RecentPostViewModel>()
            .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.Author!.UserName ?? "N/A"))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.CreatedAt));

        CreateMap<CreatePostViewModel, Post>()
            .ForMember(dest => dest.FeaturedImageUrls, opt => opt.MapFrom(src => src.FeaturedImageUrl))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Slug, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.PublishedDate, opt => opt.Ignore())
            .ForMember(dest => dest.IsPublished, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.AuthorId, opt => opt.Ignore())
            .ForMember(dest => dest.Author, opt => opt.Ignore())
            .ForMember(dest => dest.PostTags, opt => opt.Ignore())
            .ForMember(dest => dest.Category, opt => opt.Ignore());

        CreateMap<Post, EditPostViewModel>()
            .ForMember(dest => dest.SelectedTagIds,
                opt => opt.MapFrom(src => src.PostTags.Select(pt => pt.TagId).ToList()))
            .ForMember(dest => dest.FeaturedImageUrl,
                opt => opt.ConvertUsing(new SingleUrlConverter("large"), src => src.FeaturedImageUrls))
            .ForMember(dest => dest.PublishNow, opt => opt.MapFrom(src => src.IsPublished))
            .ForMember(dest => dest.AvailableTags, opt => opt.Ignore())
            .ForMember(dest => dest.AvailableCategories, opt => opt.Ignore());

        CreateMap<EditPostViewModel, Post>()
            .ForMember(dest => dest.PostTags, opt => opt.Ignore())
            .ForMember(dest => dest.FeaturedImageUrls, opt => opt.Ignore()) // Handled manually in Service to preserve JSON
            .ForMember(dest => dest.IsPublished, opt => opt.MapFrom(src => src.PublishNow))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.PublishedDate, opt => opt.Ignore())
            .ForMember(dest => dest.AuthorId, opt => opt.Ignore())
            .ForMember(dest => dest.Slug, opt => opt.Ignore())
            .ForMember(dest => dest.Author, opt => opt.Ignore())
            .ForMember(dest => dest.Category, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore());
        
        CreateMap<Post, PostViewModel>()
            .ForMember(dest => dest.FeaturedImageUrl,
                opt => opt.ConvertUsing(new SingleUrlConverter("large"), src => src.FeaturedImageUrls))
            .ForMember(dest => dest.FeaturedImageAlt, opt => opt.MapFrom(src => src.FeaturedImageAlt))
            .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.Author!.UserName ?? "N/A"))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.PostTags.Select(pt => pt.Tag!.Name).ToList()))
            .ForMember(dest => dest.PublishedDate, opt => opt.MapFrom(src => src.PublishedDate ?? src.CreatedAt))
            .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : "Uncategorized"))
            .ForMember(dest => dest.Excerpt, opt => opt.MapFrom(src => GetPlainTextExcerpt(src.Content, 200)));

        CreateMap<Post, PostCardViewModel>()
            .ForMember(dest => dest.FeaturedImageUrls,
                opt => opt.ConvertUsing(new FeaturedImageUrlConverter(), src => src.FeaturedImageUrls))
            .ForMember(dest => dest.CategoryName,
                opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : "Uncategorized"))
            .ForMember(dest => dest.CategorySlug,
                opt => opt.MapFrom(src => src.Category != null ? src.Category.Slug : string.Empty))
            .ForMember(dest => dest.PublishDate, opt => opt.MapFrom(src => src.PublishedDate ?? src.CreatedAt))
            .ForMember(dest => dest.ShortDescription, opt => opt.MapFrom(src => GetPlainTextExcerpt(src.Content, 200)))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.PostTags.Select(pt => pt.Tag)));

        CreateMap<Tag, TagViewModel>()
            .ForMember(dest => dest.PostCount, opt => opt.MapFrom(src => src.PostTags != null ? src.PostTags.Count : 0))
            .ForMember(dest => dest.Slug, opt => opt.Ignore());

        CreateMap<Tag, WebApp.Areas.Admin.ViewModels.Tag.TagViewModel>()
            .ForMember(dest => dest.PostCount, opt => opt.MapFrom(src => src.PostTags != null ? src.PostTags.Count : 0));

        CreateMap<Category, AdminCategoryViewModel>()
            .ForMember(dest => dest.PostCount, opt => opt.MapFrom(src => src.Posts != null ? src.Posts.Count : 0));

        CreateMap<Category, WebApp.ViewModels.CategoryViewModel>()
            .ForMember(dest => dest.PostCount, opt => opt.MapFrom(src => src.Posts != null ? src.Posts.Count : 0));

        CreateMap<CreateCategoryViewModel, Category>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Slug, opt => opt.Ignore())
            .ForMember(dest => dest.Posts, opt => opt.Ignore());

        CreateMap<Category, EditCategoryViewModel>();

        CreateMap<EditCategoryViewModel, Category>()
            .ForMember(dest => dest.Slug, opt => opt.Ignore())
            .ForMember(dest => dest.Posts, opt => opt.Ignore());

        CreateMap<CreateTagViewModel, Tag>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PostTags, opt => opt.Ignore());

        CreateMap<Tag, EditTagViewModel>();

        CreateMap<EditTagViewModel, Tag>()
            .ForMember(dest => dest.PostTags, opt => opt.Ignore());

        CreateMap<Tag, SelectListItem>()
            .ForMember(dest => dest.Value, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.Text, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Disabled, opt => opt.Ignore())
            .ForMember(dest => dest.Group, opt => opt.Ignore())
            .ForMember(dest => dest.Selected, opt => opt.Ignore());
    }

    private static string GetPlainTextExcerpt(string? markdown, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        var plainText = Markdown.ToPlainText(markdown);
        plainText = System.Text.RegularExpressions.Regex.Replace(plainText, @"\s+", " ").Trim();

        if (plainText.Length <= maxLength)
            return plainText;

        var lastSpace = plainText.LastIndexOf(' ', maxLength);
        if (lastSpace > maxLength / 2)
            return plainText[..lastSpace] + "...";

        return plainText[..maxLength] + "...";
    }
}

public class FeaturedImageUrlConverter : IValueConverter<string?, Dictionary<string, string>>
{
    public Dictionary<string, string> Convert(string? sourceMember, ResolutionContext context)
    {
        if (string.IsNullOrWhiteSpace(sourceMember))
            return new Dictionary<string, string>();
        
        if (sourceMember.TrimStart().StartsWith("{", StringComparison.Ordinal))
        {
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, string>>(sourceMember)
                       ?? new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }
        
        return new Dictionary<string, string>
        {
            ["large"] = sourceMember,
            ["thumbnail"] = sourceMember,
            ["original"] = sourceMember
        };
    }
}

public class SingleUrlConverter : IValueConverter<string?, string?>
{
    private readonly string _sizeKey;

    public SingleUrlConverter(string sizeKey)
    {
        _sizeKey = sizeKey;
    }

    public string? Convert(string? sourceMember, ResolutionContext context)
    {
        if (string.IsNullOrWhiteSpace(sourceMember))
            return null;
        
        if (!sourceMember.TrimStart().StartsWith("{", StringComparison.Ordinal))
            return sourceMember;

        try
        {
            var urls = JsonSerializer.Deserialize<Dictionary<string, string>>(sourceMember);
            return urls?.GetValueOrDefault(_sizeKey) ?? urls?.Values.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }
}