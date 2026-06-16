using eNote.Domain.Entities;

namespace eNote.Application.Features.Users.Services.Interfaces
{
    public interface IUserContextResolver
    {
        Task<Student> GetStudentAsync(int userId);
        Task<Instructor> GetInstructorAsync(int userId);
        Task<MusicStoreEmployee> GetActiveEmployeeAsync(int userId);
        Task<string> GetStudentDisplayNameAsync(Student student);
    }
}
