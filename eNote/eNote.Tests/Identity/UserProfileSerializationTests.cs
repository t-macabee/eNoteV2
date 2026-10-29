using System.Text.Json;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Profiles;

namespace eNote.Tests.Identity;

public class UserProfileSerializationTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void UserProfileResponse_SerializesPolymorphicProfile()
    {
        var profile = new StudentProfile(1, Now, "John", "Doe", new DateTime(2000, 1, 1), null);
        var response = new UserProfileResponse("student", "jdoe", "jdoe@example.com", profile);

        var json = JsonSerializer.Serialize(response);

        Assert.Contains("student", json);
        Assert.Contains("John", json);
        Assert.Contains("Doe", json);
        Assert.Contains("jdoe", json);
        Assert.Contains("jdoe@example.com", json);

        var deserialized = JsonSerializer.Deserialize<UserProfileResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(deserialized);
        Assert.Equal("jdoe", deserialized.Username);
        Assert.Equal("jdoe@example.com", deserialized.Email);
        Assert.IsType<StudentProfile>(deserialized.Profile);

        var studentProfile = (StudentProfile)deserialized.Profile;
        Assert.Equal("John", studentProfile.FirstName);
        Assert.Equal("Doe", studentProfile.LastName);
    }
}
