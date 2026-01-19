using MailKit;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScopeIndiaWebsite.Models
{
    public enum Gender
    {
        Male, Female, Other
    }
    public class Student
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "First Name is required")]
        public string reg_first_name { get; set; }

        [Required(ErrorMessage = "Last Name is required")]
        public string reg_last_name { get; set; }

        [Required(ErrorMessage = "Date of Birth is required")]
        public DateOnly? reg_date_of_birth { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        public Gender Gender { get; set; }

        [Required(ErrorMessage = "Phone Number is required")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Enter a valid 10-digit phone number")]
        public string reg_mobile_number { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string reg_email { get; set; }

        [Required(ErrorMessage = "Country is required")]
        public string reg_country { get; set; }

        [Required(ErrorMessage = "State is required")]
        public string reg_state { get; set; }

        [Required(ErrorMessage = "City is required")]
        public string reg_city { get; set; }

        // This must be a LIST to HOLD selected checkboxes after submit
        public List<string> SelectedHobbies { get; set; } = new();

        // Hobby database storage column
        [Required(ErrorMessage = "Please select at least one hobby")]
        public string reg_hobbies { get; set; }

        // Store image in DB
        public byte[] Avatar { get; set; }

        // Must exist for binding file input to the model
        [NotMapped]
        public IFormFile AvatarFile { get; set; }
    }
}
