// Models/RegisterRequest.cs

using System.ComponentModel.DataAnnotations;

public class RegisterRequest
{   
    [Required]
    public string Username { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; }
}
