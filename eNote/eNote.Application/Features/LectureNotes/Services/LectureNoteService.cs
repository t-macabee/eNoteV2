using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.LectureNotes.Services.Interfaces;
using eNote.Application.Features.Users;
using eNote.Domain.Entities;
using eNote.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.LectureNotes.Services
{
    public class LectureNoteService(IAppDbContext context, IClock clock) : ILectureNoteService
    {
        public async Task<PagedResult<LectureNoteDto>> GetForLectureAsync(int lectureId, int instructorUserId, int page, int pageSize)
        {
            await EnsureInstructorOwnsLectureAsync(lectureId, instructorUserId);

            var query = context.Set<LectureNote>()
                .AsNoTracking()
                .Where(x => x.LectureId == lectureId && x.IsActive);

            return await query.ToPagedResultAsync(
                page,
                pageSize,
                includeTotalCount: true,
                Map,
                q => q.OrderByDescending(x => x.CreatedAt));
        }

        public async Task<LectureNoteDto> GetByIdForInstructorAsync(int lectureId, int noteId, int instructorUserId)
        {
            await EnsureInstructorOwnsLectureAsync(lectureId, instructorUserId);

            var entity = await context.Set<LectureNote>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == noteId && x.LectureId == lectureId && x.IsActive)
                ?? throw new NotFoundException(Messages.LectureNoteNotFound);

            return Map(entity);
        }

        public async Task<LectureNoteDto> CreateAsync(int lectureId, int instructorUserId, LectureNoteCreateRequest request)
        {
            await EnsureInstructorOwnsLectureAsync(lectureId, instructorUserId);

            var entity = new LectureNote
            {
                Title = request.Title.Trim(),
                Content = request.Content.Trim(),
                LectureId = lectureId,
                CreatedById = instructorUserId
            };

            context.Set<LectureNote>().Add(entity);
            await context.SaveChangesAsync();

            return Map(entity);
        }

        public async Task<LectureNoteDto> UpdateAsync(int lectureId, int noteId, int instructorUserId, LectureNoteUpdateRequest request)
        {
            await EnsureInstructorOwnsLectureAsync(lectureId, instructorUserId);

            var entity = await context.Set<LectureNote>()
                .FirstOrDefaultAsync(x => x.Id == noteId && x.LectureId == lectureId && x.IsActive)
                ?? throw new NotFoundException(Messages.LectureNoteNotFound);

            entity.Title = request.Title.Trim();
            entity.Content = request.Content.Trim();
            entity.UpdatedAt = clock.UtcNow;
            entity.UpdatedById = instructorUserId;

            await context.SaveChangesAsync();

            return Map(entity);
        }

        public async Task DeleteAsync(int lectureId, int noteId, int instructorUserId)
        {
            await EnsureInstructorOwnsLectureAsync(lectureId, instructorUserId);

            var entity = await context.Set<LectureNote>()
                .FirstOrDefaultAsync(x => x.Id == noteId && x.LectureId == lectureId && x.IsActive)
                ?? throw new NotFoundException(Messages.LectureNoteNotFound);

            entity.IsActive = false;
            entity.UpdatedAt = clock.UtcNow;
            entity.UpdatedById = instructorUserId;

            await context.SaveChangesAsync();
        }

        public async Task<PagedResult<LectureNoteDto>> GetForStudentAsync(int lectureId, int studentUserId, int page, int pageSize)
        {
            await EnsureStudentCanAccessLectureAsync(lectureId, studentUserId);

            var query = context.Set<LectureNote>()
                .AsNoTracking()
                .Where(x => x.LectureId == lectureId && x.IsActive);

            return await query.ToPagedResultAsync(
                page,
                pageSize,
                includeTotalCount: true,
                Map,
                q => q.OrderByDescending(x => x.CreatedAt));
        }

        public async Task<LectureNoteDto> GetByIdForStudentAsync(int lectureId, int noteId, int studentUserId)
        {
            await EnsureStudentCanAccessLectureAsync(lectureId, studentUserId);

            var entity = await context.Set<LectureNote>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == noteId && x.LectureId == lectureId && x.IsActive)
                ?? throw new NotFoundException(Messages.LectureNoteNotFound);

            return Map(entity);
        }

        private async Task EnsureStudentCanAccessLectureAsync(int lectureId, int studentUserId)
        {
            var student = await UserProfileHelper.GetStudentByUserIdAsync(context, studentUserId);

            _ = await context.Set<Lecture>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == lectureId &&
                    x.IsActive &&
                    x.Course.IsActive &&
                    x.Course.IsPublished &&
                    !x.IsCancelled &&
                    x.Course.Enrollments.Any(e =>
                        e.StudentId == student.Id &&
                        e.EnrollmentStatus == EnrollmentStatus.Active))
                ?? throw new NotFoundException(Messages.LectureNotFound);
        }

        private async Task EnsureInstructorOwnsLectureAsync(int lectureId, int instructorUserId)
        {
            var instructor = await UserProfileHelper.GetInstructorByUserIdAsync(context, instructorUserId);

            _ = await context.Set<Lecture>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == lectureId && x.Course.InstructorId == instructor.Id && x.IsActive)
                ?? throw new AuthorizationException(Messages.CourseNotOwned);
        }

        private static LectureNoteDto Map(LectureNote entity) => new()
        {
            Id = entity.Id,
            Title = entity.Title,
            Content = entity.Content,
            LectureId = entity.LectureId
        };
    }
}
