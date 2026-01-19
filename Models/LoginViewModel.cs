using System.ComponentModel.DataAnnotations;

namespace ScopeIndiaWebsite.Models
{
    public class LoginViewModel
    {
        public string Email { get; set; }
        public string Password { get; set; }        // permanent password
        public bool KeepMeLoggedIn { get; set; }    // checkbox
    }
}

