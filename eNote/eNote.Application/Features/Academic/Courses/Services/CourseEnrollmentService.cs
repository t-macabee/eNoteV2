using eNote.Application.Constants;
using eNote.Application.Features.Rentals.Payments.Services;
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
        var student = await students.GetCurrentStudentAsync(cancellationToken);

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

        if (enrollment is null)
        {
            context.Set<Enrollment>().Add(new Enrollment(student.Id, courseId, EnrollmentStatus.Pending)
            {
                CreatedById = currentUser.UserId
            });
        }
        else if (enrollment.EnrollmentStatus is EnrollmentStatus.Pending or EnrollmentStatus.Active)
        {
            return;
        }
        else
        {
            var result = enrollment.Transition(EnrollmentTrigger.Request, currentUser.UserId, clock.UtcNow);

            if (!result.IsSuccess)
            {
                throw new BusinessException(result.Error);
            }
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(Messages.ConcurrencyConflict);
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex, DbConstraintNames.EnrollmentStudentIdCourseIdUniqueIndex))
        {
            throw new ConflictException(Messages.AlreadyEnrolled);
        }

        logger.LogInformation("Student {StudentUserId} requested enrollment in course {CourseId}", currentUser.UserId, courseId);
    }

    public async Task UnenrollAsync(int courseId, CancellationToken cancellationToken = default)
    {
        var student = await students.GetCurrentStudentAsync(cancellationToken);

        var enrollment = await context.Set<Enrollment>()
            .FirstOrDefaultAsync(e =>
                e.CourseId == courseId &&
                e.StudentId == student.Id &&
                (e.EnrollmentStatus == EnrollmentStatus.Active || e.EnrollmentStatus == EnrollmentStatus.Pending),
                cancellationToken)
            ?? throw new BusinessException(Messages.StudentNotEnrolled);

        var hasOpenPayment = await context.Set<CoursePayment>()
            .AnyAsync(p => p.EnrollmentId == enrollment.Id
                && p.Status == PaymentStatus.RequiresAction
                && p.CreatedAt >= clock.UtcNow - PaymentGatewayHelpers.RequiresActionReuseWindow,
                cancellationToken);

        if (hasOpenPayment)
        {
            throw new BusinessException(Messages.UnenrollBlockedByPendingPayment);
        }

        var result = enrollment.Transition(EnrollmentTrigger.Cancel, currentUser.UserId, clock.UtcNow);

        if (!result.IsSuccess)
        {
            throw new BusinessException(result.Error);
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(Messages.ConcurrencyConflict);
        }

        logger.LogInformation("Student {StudentUserId} unenrolled from course {CourseId}", currentUser.UserId, courseId);
    }
}
