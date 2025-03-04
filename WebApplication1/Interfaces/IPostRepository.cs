using System;
using WebApplication1.Models;

namespace WebApplication1.Interfaces;

public interface IPostRepository : IRepository<Post>
{
    Task<IEnumerable<Post>> GetPublishedPostsAsync(int skip, int take);
    Task<Post> GetPostWithDetailsAsync(int id);
    Task<int> CountPublishedPostsAsync();

    //TODO: Add more methods here
}
