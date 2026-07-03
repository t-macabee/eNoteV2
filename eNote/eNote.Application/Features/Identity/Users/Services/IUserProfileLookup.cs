namespace eNote.Application.Features.Identity.Users.Services;

public interface IUserProfileLookup
{
    Task<Student> GetStudentAsync(int userId);
    Task<Instructor> GetInstructorAsync(int userId);
    Task<MusicStoreEmployee> GetActiveEmployeeAsync(int userId);
}
