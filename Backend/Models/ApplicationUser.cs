﻿using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    public class ApplicationUser
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public string? Role { get; set; } = "User"; 

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
