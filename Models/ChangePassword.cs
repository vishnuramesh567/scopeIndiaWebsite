using System.ComponentModel.DataAnnotations;

public class ChangePassword
{
    [Key] // Make sure this attribute is present
    public int Id { get; set; }

    [Required]
    public string? Email { get; set; }

    [Required]
    public string? Password { get; set; }
}