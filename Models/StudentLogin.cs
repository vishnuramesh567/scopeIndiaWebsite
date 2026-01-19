using System.ComponentModel.DataAnnotations;

namespace ScopeIndiaWebsite.Models
{
    public class StudentLogin
    {
        public int Id { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        // hashed password
        public string PasswordHash { get; set; }

        // temporary plain temp password (8 chars) — stored only briefly
        public string TempPassword { get; set; }

        // used to know if first-time flow applies (optional)
        public bool IsFirstLogin { get; set; } = true;
    }
}
