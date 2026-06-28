using eNote.Domain.Entities;

namespace eNote.Application.Common.Interfaces;

public interface ICurrentActor : ICurrentUserService
{
    Task<Student> GetStudentAsync();
    Task<int> GetCurrentStudentIdAsync();
    Task<Instructor> GetInstructorAsync();
    Task<MusicStoreEmployee> GetActiveEmployeeAsync();
    Task<int> GetActiveStoreAsync();
}
