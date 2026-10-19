using System.Reflection;
using eNote.API.Controllers.Files;
using eNote.Application.Common.Files;
using eNote.Infrastructure.Storage;
using Microsoft.AspNetCore.Mvc;

namespace eNote.Tests.Storage;

public sealed class DefaultImagesTests
{
    [Theory]
    [InlineData(DefaultImages.Announcement)]
    [InlineData(DefaultImages.Course)]
    [InlineData(DefaultImages.Event)]
    [InlineData(DefaultImages.MusicStore)]
    [InlineData(DefaultImages.UserPicture)]
    public void DefaultImage_IsServedFromUploadsRoute(string path)
    {
        Assert.StartsWith(LocalFileStorageService.UploadsRoutePrefix + "/", path);
    }

    [Theory]
    [InlineData(DefaultImages.Announcement)]
    [InlineData(DefaultImages.Course)]
    [InlineData(DefaultImages.Event)]
    [InlineData(DefaultImages.MusicStore)]
    public void DefaultImageFolder_HasUploadsControllerRoute(string path)
    {
        var folder = path[(LocalFileStorageService.UploadsRoutePrefix.Length + 1)..].Split('/')[0];

        var templates = typeof(UploadsController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .SelectMany(method => method.GetCustomAttributes<HttpGetAttribute>())
            .Select(attribute => attribute.Template);

        Assert.Contains($"{folder}/{{fileName}}", templates);
    }
}
