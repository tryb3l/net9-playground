using AutoMapper;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApplication1.Areas.Admin.ViewModels.Dashboard;
using WebApplication1.Areas.Admin.ViewModels.Post;
using WebApplication1.Models;
using WebApplication1.ViewModels;

namespace WebApplication1.Areas.Admin.Mapping;

public class AdminMappingProfile : Profile
{
    public AdminMappingProfile()
    {
        CreateMap<Post, RecentPostViewModel>()
            .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.Author!.UserName ?? "N/A"));

        CreateMap<CreatePostViewModel, Post>()
            .ForMember(dest => dest.FeaturedImageUrl, opt => opt.MapFrom(src => src.FeaturedImageUrl));

        CreateMap<Post, EditPostViewModel>()
            .ForMember(dest => dest.SelectedTagIds, opt => opt.MapFrom(src => src.PostTags.Select(pt => pt.TagId).ToList()))
            .ForMember(dest => dest.FeaturedImageUrl, opt => opt.MapFrom(src => src.FeaturedImageUrl));

        CreateMap<EditPostViewModel, Post>()
            .ForMember(dest => dest.PostTags, opt => opt.Ignore())
            .ForMember(dest => dest.FeaturedImageUrl, opt => opt.MapFrom(src => src.FeaturedImageUrl));

        CreateMap<Post, PostViewModel>()
            .ForMember(dest => dest.FeaturedImageUrl, opt => opt.MapFrom(src => src.FeaturedImageUrl))
            .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.Author!.UserName ?? "N/A"))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.PostTags.Select(pt => pt.Tag!.Name).ToList()));

        CreateMap<Post, PostCardViewModel>()
            .ForMember(dest => dest.FeaturedImageUrl, opt => opt.MapFrom(src => src.FeaturedImageUrl))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : "Uncategorized"))
            .ForMember(dest => dest.CategorySlug, opt => opt.MapFrom(src => src.Category != null ? src.Category.Slug : string.Empty))
            .ForMember(dest => dest.PublishDate, opt => opt.MapFrom(src => src.PublishedDate ?? src.CreatedAt))
            .ForMember(dest => dest.ShortDescription, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.Content) && src.Content.Length > 200
                    ? src.Content.Substring(0, 200) + "..."
                    : src.Content ?? string.Empty))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.PostTags.Select(pt => pt.Tag)));

        CreateMap<Post, PostSummaryViewModel>()
            .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.Author!.UserName ?? "N/A"))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.PostTags.Select(pt => pt.Tag!.Name).ToList()));

        CreateMap<Tag, TagViewModel>()
            .ForMember(dest => dest.PostCount, opt => opt.MapFrom(src => src.PostTags.Count));
        
        CreateMap<Category, WebApplication1.ViewModels.CategoryViewModel>()
            .ForMember(dest => dest.PostCount, opt => opt.MapFrom(src => src.Posts.Count(p => p.IsPublished && !p.IsDeleted)));
        
        CreateMap<Category, WebApplication1.Areas.Admin.ViewModels.Category.CategoryViewModel>()
            .ForMember(dest => dest.PostCount, opt => opt.MapFrom(src => src.Posts.Count));

        CreateMap<Tag, SelectListItem>()
            .ForMember(dest => dest.Value, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.Text, opt => opt.MapFrom(src => src.Name));
    }
}