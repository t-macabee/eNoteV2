namespace eNote.Application.Features.Identity.Users.Services;

public interface IStudentDisplayNameService
{
    Task<string> GetStudentDisplayNameAsync(Student student, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<int, string>> GetStudentDisplayNamesAsync(IEnumerable<Student> students, CancellationToken cancellationToken = default);
}
