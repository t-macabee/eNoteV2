namespace eNote.Application.Features.Identity.Users.Services;

public interface IUserProfileLookup
{
    Task<Student> GetStudentAsync(int userId, CancellationToken cancellationToken = default);
    Task<Instructor> GetInstructorAsync(int userId, CancellationToken cancellationToken = default);
    Task<MusicStoreEmployee> GetEmployeeAsync(int userId, CancellationToken cancellationToken = default);
}
