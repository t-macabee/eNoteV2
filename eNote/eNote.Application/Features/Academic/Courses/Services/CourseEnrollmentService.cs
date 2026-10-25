using eNote.Application.Constants;
using eNote.Application.Features.Rentals.Payments.Services;
using Microsoft.Extensions.Logging;

namespace eNote.Application.Features.Academic.Courses.Services;

public sealed class CourseEnrollmentService(
    IAppDbContext context,
    IClock clock,
    ICurrentUserContext currentUser, IStudentContext students,
    IPaymentGateway paymentGateway,
    ILogger<CourseEnrollmentService> logger)
{
    public async Task EnrollAsync(int courseId, CancellationToken cancellationToken = default)
    {
        var student = await students.GetCurrentStudentAsync(cancellationToken);

        if (!student.HasActiveMembership(clock.UtcNow))
        {
            throw new BusinessException(Messages.MembershipInactive);
        }

        var course = await context.Set<Course>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId && c.IsPublished, cancellationToken)
            ?? throw new NotFoundException(Messages.CourseNotFound);

        if (course.HasEnded(clock.UtcNow))
        {
            throw new BusinessException(Messages.CourseEnded);
        }

        var enrollment = await context.Set<Enrollment>()
            .FirstOrDefaultAsync(e => e.StudentId == student.Id && e.CourseId == courseId, cancellationToken);

        if (enrollment is null)
        {
            context.Set<Enrollment>().Add(new Enrollment(student.Id, courseId, EnrollmentStatus.Pending));
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

        var openAttempts = await context.Set<CoursePayment>()
            .Where(p => p.EnrollmentId == enrollment.Id
                && (p.Status == PaymentStatus.RequiresAction || p.Status == PaymentStatus.Failed))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        await PaymentGatewayHelpers.ResolveOpenAttemptsAsync(
            logger,
            context,
            paymentGateway,
            openAttempts,
            expected: null,
            Messages.UnenrollBlockedByPendingPayment,
            cancellationToken);

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
