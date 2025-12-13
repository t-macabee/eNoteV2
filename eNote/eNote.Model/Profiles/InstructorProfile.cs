namespace eNote.Model.Profiles
{
    public record InstructorProfile(int Id, string? FirstName, string? LastName) : IUserProfile;    
}
