// Models/User.cs
using Microsoft.AspNetCore.Identity;

namespace TaskManagementApi.Models
{
    public class UserModel : IdentityUser
    {
        // You can add additional properties here
        public string? Role { get; set; }
    }
}
