using System.Text.Json.Serialization;

namespace eNote.Application.Features.Identity.Users.Profiles;

[JsonPolymorphic]
[JsonDerivedType(typeof(StudentProfile), "student")]
[JsonDerivedType(typeof(InstructorProfile), "instructor")]
[JsonDerivedType(typeof(MusicStoreProfile), "storeemployee")]
[JsonDerivedType(typeof(AdminProfile), "admin")]
public interface IUserProfile
{
}
