using eNote.Domain.Entities;

namespace eNote.Application.Features.Users.Services
{
    public interface IUserContextResolver
    {
        Task<Student> GetStudentAsync(int userId);
        Task<Instructor> GetInstructorAsync(int userId);
        Task<MusicStoreEmployee> GetActiveEmployeeAsync(int userId);
        Task<string> GetStudentDisplayNameAsync(Student student);
        Task<IReadOnlyDictionary<int, string>> GetStudentDisplayNamesAsync(IEnumerable<Student> students);
    }
}
