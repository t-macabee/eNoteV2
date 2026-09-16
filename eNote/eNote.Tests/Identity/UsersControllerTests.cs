using eNote.API.Controllers.Users;
using eNote.Application.Common.Localization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.Tests.Identity;

public sealed class UsersControllerTests
{
    [Fact]
    public async Task UploadPicture_ReturnsBadRequest_WhenFilePartMissing()
    {
        var controller = new UsersController(null!, null!, null!, null!);

        var result = await controller.UploadPicture(null);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains(Messages.FileNotProvided, badRequest.Value!.ToString());
    }
}
