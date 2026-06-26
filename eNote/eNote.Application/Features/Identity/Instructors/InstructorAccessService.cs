using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Domain.Entities;
using eNote.Domain.Entities.Assignments;
using eNote.Domain.Entities.Communication;
using eNote.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Identity.Instructors;

public sealed class InstructorAccessService(IAppDbContext context, IUserContextResolver resolver) : IInstructorAccessService
{
    public Task<Instructor> GetInstructorAsync(int userId) => resolver.GetInstructorAsync(userId);
    public async Task<int> GetCurrentInstructorIdAsync(int appUserId) => (await GetInstructorAsync(appUserId)).Id;

    public Task<bool> OwnsCourseAsync(int courseId, int instructorId) =>
        context.Set<Course>().AnyAsync(c => c.Id == courseId && c.InstructorId == instructorId);

    public async Task EnsureOwnsCourseAsync(int courseId, int instructorId)
    {
        if (!await OwnsCourseAsync(courseId, instructorId))
        {
            throw new AuthorizationException(Messages.CourseNotOwned);
        }
    }

    public async Task EnsureOwnsLectureAsync(int lectureId, int instructorId)
    {
        if (!await context.Set<Lecture>().AnyAsync(x => x.Id == lectureId && x.Course.InstructorId == instructorId))
        {
            throw new AuthorizationException(Messages.CourseNotOwned);
        }
    }

    public async Task<Lecture> GetOwnedLectureAsync(int lectureId, int instructorId, bool track = false, bool includeAttendances = false)
    {
        var query = context.Set<Lecture>()
            .Where(x => x.Id == lectureId && x.Course.InstructorId == instructorId);

        if (includeAttendances)
        {
            query = query.Include(x => x.Attendances);
        }

        query = track ? query : query.AsNoTracking();

        return await query.FirstOrDefaultAsync()
            ?? throw new NotFoundException(Messages.LectureNotFound);
    }

    public async Task<Assignment> GetOwnedAssignmentAsync(int lectureId, int assignmentId, int instructorId, bool track = false)
    {
        var query = context.Set<Assignment>()
            .Where(x => x.Id == assignmentId && x.LectureId == lectureId && x.Lecture.Course.InstructorId == instructorId);

        query = track ? query : query.AsNoTracking();

        return await query.FirstOrDefaultAsync()
            ?? throw new NotFoundException(Messages.AssignmentNotFound);
    }

    public async Task<LectureNote> GetOwnedLectureNoteAsync(int lectureId, int noteId, int instructorId, bool track = false)
    {
        var query = context.Set<LectureNote>()
            .Where(x => x.Id == noteId && x.LectureId == lectureId && x.Lecture.Course.InstructorId == instructorId);

        query = track ? query : query.AsNoTracking();

        return await query.FirstOrDefaultAsync()
            ?? throw new NotFoundException(Messages.LectureNoteNotFound);
    }

    public IQueryable<Course> CoursesFor(int instructorId) =>
        context.Set<Course>().Where(c => c.InstructorId == instructorId);

    public IQueryable<Lecture> LecturesFor(int instructorId) =>
        context.Set<Lecture>().Where(x => x.Course.InstructorId == instructorId);

    public IQueryable<Assignment> AssignmentsForLecture(int lectureId, int instructorId) =>
        context.Set<Assignment>().Where(x => x.LectureId == lectureId && x.Lecture.Course.InstructorId == instructorId);

    public IQueryable<LectureNote> LectureNotesForLecture(int lectureId, int instructorId) =>
        context.Set<LectureNote>().Where(x => x.LectureId == lectureId && x.Lecture.Course.InstructorId == instructorId);

    public IQueryable<Announcement> CourseAnnouncementsFor(int courseId, int instructorId, bool track = false)
    {
        var query = context.Set<Announcement>()
            .Include(a => a.Course)
            .Where(a => a.CourseId == courseId && a.Course!.InstructorId == instructorId);

        return track ? query : query.AsNoTracking();
    }
}
