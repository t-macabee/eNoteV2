using eNote.Application.Constants;
using eNote.Application.Features.Payments;
using Microsoft.Extensions.Logging;

namespace eNote.Application.Features.Academic.Courses.Services;

public sealed class CourseEnrollmentService(
    IAppDbContext context,
    IClock clock,
    ICurrentUserContext currentUser, IStudentContext students,
    IPaymentGateway paymentGateway,
    ILogger<CourseEnrollmentService> logger)
{
    public async Task<EnrollmentStatus> EnrollAsync(int courseId, CancellationToken cancellationToken = default)
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

        var resumed = false;
        var status = EnrollmentStatus.Pending;

        if (enrollment is null)
        {
            context.Set<Enrollment>().Add(new Enrollment(student.Id, courseId, EnrollmentStatus.Pending));
        }
        else if (enrollment.EnrollmentStatus is EnrollmentStatus.Pending or EnrollmentStatus.Active)
        {
            return enrollment.EnrollmentStatus;
        }
        else
        {
            var trigger = enrollment.CanResume(clock.UtcNow)
                ? EnrollmentTrigger.Resume
                : EnrollmentTrigger.Request;
            var result = enrollment.Transition(trigger, currentUser.UserId, clock.UtcNow);

            if (!result.IsSuccess)
            {
                throw new BusinessException(result.Error);
            }

            resumed = trigger == EnrollmentTrigger.Resume;
            status = result.Value;
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

        logger.LogInformation("Student {StudentUserId} {EnrollmentAction} enrollment in course {CourseId}", currentUser.UserId, resumed ? "resumed" : "requested", courseId);

        return status;
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
                && StripePayment.OpenStatuses.Contains(p.Status))
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
