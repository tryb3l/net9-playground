using System.Text.Json;
using AutoMapper;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApp.Areas.Admin.ViewModels.Dashboard;
using WebApp.Areas.Admin.ViewModels.Post;
using WebApp.Models;
using WebApp.ViewModels;
using Tag_TagViewModel = WebApp.Areas.Admin.ViewModels.Tag.TagViewModel;
using TagViewModel = WebApp.Areas.Admin.ViewModels.Tag.TagViewModel;
using ViewModels_TagViewModel = WebApp.ViewModels.TagViewModel;

namespace WebApp.Areas.Admin.Mapping;

public class FeaturedImageUrlConverter : IValueConverter<string?, Dictionary<string, string>>
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Dictionary<string, string> Convert(string? sourceMember, ResolutionContext context)
    {
        if (string.IsNullOrEmpty(sourceMember))
        {
            return [];
        }

        if (sourceMember.Trim().StartsWith("{"))
        {
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, string>>(sourceMember, JsonSerializerOptions)
                       ?? new Dictionary<string, string>();
            }
            catch (JsonException)
            {
                return [];
            }
        }

        return new Dictionary<string, string> { { "medium", sourceMember } };
    }
}

public class SingleUrlConverter : IValueConverter<string?, string>
{
    private readonly string _key;
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public SingleUrlConverter(string key)
    {
        _key = key;
    }

    public string Convert(string? sourceMember, ResolutionContext context)
    {
        if (string.IsNullOrEmpty(sourceMember))
        {
            return string.Empty;
        }

        if (sourceMember.Trim().StartsWith("{"))
        {
            try
            {
                var urls = JsonSerializer.Deserialize<Dictionary<string, string>>(sourceMember, JsonSerializerOptions);
                if (urls != null && urls.TryGetValue(_key, out var url))
                {
                    return url;
                }
            }
            catch (JsonException)
            {
                return string.Empty;
            }
        }
        else
        {
            return sourceMember;
        }

        return string.Empty;
    }
}


public class AdminMappingProfile : Profile
{
    public AdminMappingProfile()
    {
        CreateMap<Post, RecentPostViewModel>()
            .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.Author!.UserName ?? "N/A"));

        CreateMap<CreatePostViewModel, Post>()
            .ForMember(dest => dest.FeaturedImageUrls, opt => opt.MapFrom(src => src.FeaturedImageUrl));

        CreateMap<Post, EditPostViewModel>()
            .ForMember(dest => dest.SelectedTagIds,
                opt => opt.MapFrom(src => src.PostTags.Select(pt => pt.TagId).ToList()))
            .ForMember(dest => dest.FeaturedImageUrl, opt => opt.ConvertUsing(new SingleUrlConverter("large"), src => src.FeaturedImageUrls));

        CreateMap<EditPostViewModel, Post>()
            .ForMember(dest => dest.PostTags, opt => opt.Ignore())
            .ForMember(dest => dest.FeaturedImageUrls, opt => opt.MapFrom(src => src.FeaturedImageUrl));

        CreateMap<Post, PostViewModel>()
            .ForMember(dest => dest.FeaturedImageUrl, opt => opt.ConvertUsing(new SingleUrlConverter("large"), src => src.FeaturedImageUrls))
            .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.Author!.UserName ?? "N/A"))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.PostTags.Select(pt => pt.Tag!.Name).ToList()))
            .ForMember(dest => dest.PublishedDate, opt => opt.MapFrom(src => src.PublishedDate ?? src.CreatedAt));

        CreateMap<Post, PostCardViewModel>()
            .ForMember(dest => dest.FeaturedImageUrls,
                opt => opt.ConvertUsing(new FeaturedImageUrlConverter(), src => src.FeaturedImageUrls))
            .ForMember(dest => dest.CategoryName,
                opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : "Uncategorized"))
            .ForMember(dest => dest.CategorySlug,
                opt => opt.MapFrom(src => src.Category != null ? src.Category.Slug : string.Empty))
            .ForMember(dest => dest.PublishDate, opt => opt.MapFrom(src => src.PublishedDate ?? src.CreatedAt))
            .ForMember(dest => dest.ShortDescription, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.Content) && src.Content.Length > 200
                    ? src.Content.Substring(0, 200) + "..."
                    : src.Content ?? string.Empty))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.PostTags.Select(pt => pt.Tag)));

        CreateMap<Post, PostSummaryViewModel>()
            .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.Author!.UserName ?? "N/A"))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.PostTags.Select(pt => pt.Tag!.Name).ToList()));

        CreateMap<Tag, ViewModels_TagViewModel>()
            .ForMember(dest => dest.PostCount, opt => opt.MapFrom(src => src.PostTags.Count));

        CreateMap<Tag, Tag_TagViewModel>()
            .ForMember(dest => dest.PostCount, opt => opt.MapFrom(src => src.PostTags.Count));

        CreateMap<Category, CategoryViewModel>()
            .ForMember(dest => dest.PostCount,
                opt => opt.MapFrom(src => src.Posts.Count(p => p.IsPublished && !p.IsDeleted)));

        CreateMap<Category, ViewModels.Category.CategoryViewModel>()
            .ForMember(dest => dest.PostCount, opt => opt.MapFrom(src => src.Posts.Count));

        CreateMap<Tag, SelectListItem>()
            .ForMember(dest => dest.Value, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.Text, opt => opt.MapFrom(src => src.Name));
    }
}