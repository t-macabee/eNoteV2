using eNote.API.Services;
using Microsoft.AspNetCore.Http;

namespace eNote.Tests.Api;

public sealed class CurrentUserServiceTests
{
    [Fact]
    public void IsAuthenticated_IsFalse_WhenThereIsNoHttpContext()
    {
        var service = new CurrentUserService(new HttpContextAccessor());

        Assert.False(service.IsAuthenticated);
    }
}
