using System.ComponentModel.DataAnnotations;

namespace ScopeIndiaWebsite.Models
{
    public class VerifyEmail
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]

        public string Email { get; set; }
    }
}