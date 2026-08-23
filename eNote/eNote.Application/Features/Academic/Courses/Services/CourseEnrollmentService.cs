using Microsoft.Extensions.Logging;

namespace eNote.Application.Features.Academic.Courses.Services;

public sealed class CourseEnrollmentService(
    IAppDbContext context,
    IClock clock,
    ICurrentUserContext currentUser, IStudentContext students,
    ILogger<CourseEnrollmentService> logger)
{
    public async Task EnrollAsync(int courseId, CancellationToken cancellationToken = default)
    {
        var student = await students.GetCurrentStudentAsync();

        if (!student.HasActiveMembership(clock.UtcNow))
        {
            throw new BusinessException(Messages.MembershipInactive);
        }

        _ = await context.Set<Course>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId && c.IsPublished, cancellationToken)
            ?? throw new NotFoundException(Messages.CourseNotFound);

        var enrollment = await context.Set<Enrollment>()
            .FirstOrDefaultAsync(e => e.StudentId == student.Id && e.CourseId == courseId, cancellationToken);

        if (enrollment?.EnrollmentStatus == EnrollmentStatus.Active)
        {
            return;
        }

        if (enrollment?.EnrollmentStatus == EnrollmentStatus.Canceled)
        {
            enrollment.UpdateStatus(EnrollmentStatus.Active);
            enrollment.UpdatedById = currentUser.UserId;
        }
        else
        {
            context.Set<Enrollment>().Add(new Enrollment(student.Id, courseId, EnrollmentStatus.Active)
            {
                CreatedById = currentUser.UserId
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Student {StudentUserId} enrolled in course {CourseId}", currentUser.UserId, courseId);
    }

    public async Task UnenrollAsync(int courseId, CancellationToken cancellationToken = default)
    {
        var student = await students.GetCurrentStudentAsync();

        var enrollment = await context.Set<Enrollment>()
            .FirstOrDefaultAsync(e =>
                e.CourseId == courseId &&
                e.StudentId == student.Id &&
                e.EnrollmentStatus == EnrollmentStatus.Active,
                cancellationToken)
            ?? throw new BusinessException(Messages.StudentNotEnrolled);

        enrollment.UpdateStatus(EnrollmentStatus.Canceled);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Student {StudentUserId} unenrolled from course {CourseId}", currentUser.UserId, courseId);
    }
}
