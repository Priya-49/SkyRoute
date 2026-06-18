using Microsoft.AspNetCore.Identity;
using SkyRoute.Infrastructure.Data;

namespace SkyRoute.Tests.Infrastructure;

public sealed class ApplicationUserTests
{
    [Fact]
    public void ApplicationUser_InheritsIdentityUser()
    {
        var user = new ApplicationUser
        {
            UserName = "user@example.com",
            Email = "user@example.com"
        };

        Assert.IsAssignableFrom<IdentityUser>(user);
        Assert.Equal("user@example.com", user.Email);
    }
}