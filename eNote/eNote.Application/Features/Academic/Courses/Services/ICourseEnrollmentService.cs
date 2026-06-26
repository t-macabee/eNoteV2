namespace eNote.Application.Features.Academic.Courses.Services;

public interface ICourseEnrollmentService
{
    Task EnrollAsync(int courseId);
    Task UnenrollAsync(int courseId);
}
