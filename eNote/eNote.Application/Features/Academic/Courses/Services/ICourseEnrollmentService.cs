namespace eNote.Application.Features.Academic.Courses.Services;

public interface ICourseEnrollmentService
{
    Task EnrollAsync(int courseId, CancellationToken cancellationToken = default);
    Task UnenrollAsync(int courseId, CancellationToken cancellationToken = default);
}
