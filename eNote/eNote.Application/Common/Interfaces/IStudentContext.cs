namespace eNote.Application.Common.Interfaces;

public interface IStudentContext
{
    Task<Student> GetCurrentStudentAsync();
    Task<int> GetCurrentStudentIdAsync();
    Task<Instructor> GetCurrentInstructorAsync();
    Task<MusicStoreEmployee> GetCurrentEmployeeAsync();
}
