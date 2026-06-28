using eNote.Domain.Entities;

namespace eNote.Application.Features.Identity.Users.Services;

public interface IStudentDisplayNameService
{
    Task<string> GetStudentDisplayNameAsync(Student student);
    Task<IReadOnlyDictionary<int, string>> GetStudentDisplayNamesAsync(IEnumerable<Student> students);
}
