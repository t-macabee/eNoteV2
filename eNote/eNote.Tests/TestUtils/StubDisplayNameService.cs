using eNote.Application.Features.Identity.Users.Services;

namespace eNote.Tests.TestUtils;

public sealed class StubDisplayNameService : IStudentDisplayNameService
{
    public Task<string> GetStudentDisplayNameAsync(Student student) => Task.FromResult($"Student {student.Id}");

    public Task<IReadOnlyDictionary<int, string>> GetStudentDisplayNamesAsync(IEnumerable<Student> students) =>
        Task.FromResult<IReadOnlyDictionary<int, string>>(students.ToDictionary(s => s.Id, s => $"Student {s.Id}"));
}
