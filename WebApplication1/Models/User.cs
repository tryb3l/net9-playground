using System;
using Microsoft.AspNetCore.Identity;

namespace WebApplication1.Models;

public class User : IdentityUser
{
    public string? DisplayName { get; init; }
    public ICollection<Post> Posts { get; set; } = new List<Post>();
}
