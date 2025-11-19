using Microsoft.AspNetCore.Identity;

namespace AuthServer.Data;

public class ApplicationUser : IdentityUser
{
    public DateTime DateOfBirth { get; set; }
}
