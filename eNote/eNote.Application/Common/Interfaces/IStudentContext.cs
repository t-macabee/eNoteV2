namespace eNote.Application.Common.Interfaces;

public interface IStudentContext
{
    Task<Student> GetCurrentStudentAsync(CancellationToken cancellationToken = default);
    Task<int> GetCurrentStudentIdAsync(CancellationToken cancellationToken = default);
    Task<MusicStoreEmployee> GetCurrentEmployeeAsync(CancellationToken cancellationToken = default);
}
