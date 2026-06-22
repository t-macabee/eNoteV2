namespace eNote.Application.Features.Courses.Services;

public interface ICourseEnrollmentService
{
    Task EnrollAsync(int courseId);
    Task UnenrollAsync(int courseId);
}
