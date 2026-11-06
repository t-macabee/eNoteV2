using System.Text.Json;
using eNote.Application.Features.Identity.Users;

namespace eNote.Tests.Identity;

public sealed class UserStatusRequestTests
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"other\":true}")]
    public void Deserialize_Fails_WhenIsActiveIsMissing(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<UserStatusRequest>(json, WebOptions));
    }

    [Theory]
    [InlineData("{\"isActive\":true}", true)]
    [InlineData("{\"isActive\":false}", false)]
    public void Deserialize_ReadsIsActive_WhenPresent(string json, bool expected)
    {
        Assert.Equal(expected, JsonSerializer.Deserialize<UserStatusRequest>(json, WebOptions)!.IsActive);
    }
}
