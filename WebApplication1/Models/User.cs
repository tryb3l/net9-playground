using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace WebApplication1.Models;

public class User : IdentityUser
{
    [MaxLength(64)]
    public string? DisplayName { get; init; }
    public ICollection<Post> Posts { get; init; } = new List<Post>();
}
