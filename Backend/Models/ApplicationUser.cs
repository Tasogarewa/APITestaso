using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    public class ApplicationUser:IdentityUser
    {
        public string? DatabaseConnectionString { get; set; }
    }
}
