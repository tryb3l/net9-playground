using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

public class ActivityLog
{
    public int Id { get; init; }

    [Required]
    [MaxLength(128)]
    public required string UserId { get; init; }
    public User? User { get; init; }

    [Required]
    [MaxLength(50)]
    public required string ActionType { get; init; }

    [Required]
    [MaxLength(50)]
    public required string EntityType { get; init; }

    [Required]
    [MaxLength(500)]
    public required string Description { get; init; }

    [Required]
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}