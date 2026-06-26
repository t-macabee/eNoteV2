using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Domain.Entities;
using eNote.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eNote.Application.Features.Academic.Courses.Services;

public sealed class CourseEnrollmentService(
    IAppDbContext context,
    IClock clock,
    IUserContextResolver resolver,
    ICurrentUserService currentUserService,
    ILogger<CourseEnrollmentService> logger) : ICourseEnrollmentService
{
    public async Task EnrollAsync(int courseId)
    {
        var student = await resolver.GetStudentAsync(currentUserService.UserId);

        if (!student.HasActiveMembership(clock.UtcNow))
        {
            throw new BusinessException(Messages.MembershipInactive);
        }

        _ = await context.Set<Course>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId && c.IsPublished)
            ?? throw new NotFoundException(Messages.CourseNotFound);

        var enrollment = await context.Set<Enrollment>()
            .FirstOrDefaultAsync(e => e.StudentId == student.Id && e.CourseId == courseId);

        if (enrollment?.EnrollmentStatus == EnrollmentStatus.Active)
        {
            return;
        }

        if (enrollment?.EnrollmentStatus == EnrollmentStatus.Canceled)
        {
            enrollment.UpdateStatus(EnrollmentStatus.Active);
            enrollment.UpdatedById = currentUserService.UserId;
        }
        else
        {
            context.Set<Enrollment>().Add(new Enrollment(student.Id, courseId, EnrollmentStatus.Active)
            {
                CreatedById = currentUserService.UserId
            });
        }

        await context.SaveChangesAsync();

        logger.LogInformation("Student {StudentUserId} enrolled in course {CourseId}", currentUserService.UserId, courseId);
    }

    public async Task UnenrollAsync(int courseId)
    {
        var studentId = await resolver.GetCurrentStudentIdAsync(currentUserService.UserId);

        var enrollment = await context.Set<Enrollment>()
            .FirstOrDefaultAsync(e =>
                e.CourseId == courseId &&
                e.StudentId == studentId &&
                e.EnrollmentStatus == EnrollmentStatus.Active)
            ?? throw new BusinessException(Messages.StudentNotEnrolled);

        enrollment.UpdateStatus(EnrollmentStatus.Canceled);
        await context.SaveChangesAsync();

        logger.LogInformation("Student {StudentUserId} unenrolled from course {CourseId}", currentUserService.UserId, courseId);
    }
}
