using eNote.Application.Features.Identity.Users.Services;

namespace eNote.Tests.TestUtils;

public sealed class StubUserProfileLookup : IUserProfileLookup
{
    private readonly Student? _student;
    private readonly Instructor? _instructor;
    private readonly MusicStoreEmployee? _employee;

    public StubUserProfileLookup(Student? student = null, Instructor? instructor = null, MusicStoreEmployee? employee = null)
    {
        _student = student;
        _instructor = instructor;
        _employee = employee;
    }

    public Task<Student> GetStudentAsync(int userId) =>
        Task.FromResult(_student ?? throw new NotFoundException("Student profile not found."));

    public Task<Instructor> GetInstructorAsync(int userId) =>
        Task.FromResult(_instructor ?? throw new NotFoundException("Instructor profile not found."));

    public Task<MusicStoreEmployee> GetActiveEmployeeAsync(int userId) =>
        Task.FromResult(_employee ?? throw new NotFoundException("Employee profile not found."));
}
