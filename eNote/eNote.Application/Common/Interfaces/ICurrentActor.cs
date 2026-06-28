using eNote.Domain.Entities;

namespace eNote.Application.Common.Interfaces;

public interface ICurrentActor : ICurrentUserService
{
    Task<Student> GetCurrentStudentAsync();
    Task<int> GetCurrentStudentIdAsync();
    Task<Instructor> GetCurrentInstructorAsync();
    Task<MusicStoreEmployee> GetCurrentEmployeeAsync();
    Task<int> GetCurrentStoreIdAsync();
}
