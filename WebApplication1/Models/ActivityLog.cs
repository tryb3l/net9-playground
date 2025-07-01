using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

    public class ActivityLog
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }
        public User User { get; set; } 

        [Required]
        public string ActionType { get; set; }

        [Required]
        public string EntityType { get; set; }

        public string Description { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }