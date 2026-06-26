using eNote.Domain.Entities.Identity;

namespace eNote.Application.Features.Identity.Users.Services;

public interface IUserContextResolver
{
    Task<Student> GetStudentAsync(int userId);
    Task<int> GetCurrentStudentIdAsync(int appUserId);
    Task<Instructor> GetInstructorAsync(int userId);
    Task<MusicStoreEmployee> GetActiveEmployeeAsync(int userId);
    Task<string> GetStudentDisplayNameAsync(Student student);
    Task<IReadOnlyDictionary<int, string>> GetStudentDisplayNamesAsync(IEnumerable<Student> students);
}
