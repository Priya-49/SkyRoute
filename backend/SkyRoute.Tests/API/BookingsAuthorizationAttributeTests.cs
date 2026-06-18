using Microsoft.AspNetCore.Authorization;
using SkyRoute.API.Controllers;

namespace SkyRoute.Tests.API;

public sealed class BookingsAuthorizationAttributeTests
{
    [Fact]
    public void CreateEndpoint_IsProtectedWithAuthorizeAttribute()
    {
        var method = typeof(BookingsController).GetMethod("Create");

        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).SingleOrDefault());
    }
}
