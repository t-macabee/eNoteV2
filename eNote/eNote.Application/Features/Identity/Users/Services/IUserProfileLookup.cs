namespace eNote.Application.Features.Identity.Users.Services;

public interface IUserProfileLookup
{
    Task<Student> GetStudentAsync(int userId, CancellationToken cancellationToken = default);
    Task<Instructor> GetInstructorAsync(int userId, CancellationToken cancellationToken = default);
    Task<MusicStoreEmployee> GetActiveEmployeeAsync(int userId, CancellationToken cancellationToken = default);
}
