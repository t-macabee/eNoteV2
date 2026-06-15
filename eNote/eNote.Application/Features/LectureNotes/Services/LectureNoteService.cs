using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
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
    public class LectureNoteService(IAppDbContext context, IClock clock, ICurrentUserService currentUserService) : ILectureNoteService
    {
        public async Task<PagedResult<LectureNoteDto>> GetForLectureAsync(int lectureId, int page, int pageSize)
        {
            var instructor = await UserProfileHelper.GetInstructorByUserIdAsync(context, currentUserService.UserId);

            var query = context.Set<LectureNote>()
                .AsNoTracking()
                .Where(x => x.LectureId == lectureId && x.Lecture.Course.InstructorId == instructor.Id);

            return await query.ToPagedResultAsync(
                page,
                pageSize,
                includeTotalCount: true,
                Map,
                q => q.OrderByDescending(x => x.CreatedAt));
        }

        public async Task<LectureNoteDto> GetByIdForInstructorAsync(int lectureId, int noteId)
        {
            var entity = await GetNoteForInstructorAsync(lectureId, noteId, currentUserService.UserId);
            return Map(entity);
        }

        public async Task<LectureNoteDto> CreateAsync(int lectureId, LectureNoteCreateRequest request)
        {
            var instructor = await UserProfileHelper.GetInstructorByUserIdAsync(context, currentUserService.UserId);

            _ = await context.Set<Lecture>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == lectureId && x.Course.InstructorId == instructor.Id)
                ?? throw new eNote.Application.Common.Exceptions.AuthorizationException(Messages.CourseNotOwned);

            var entity = new LectureNote
            {
                Title = request.Title.Trim(),
                Content = request.Content.Trim(),
                LectureId = lectureId,
                CreatedById = currentUserService.UserId
            };

            context.Set<LectureNote>().Add(entity);
            await context.SaveChangesAsync();

            return Map(entity);
        }

        public async Task<LectureNoteDto> UpdateAsync(int lectureId, int noteId, LectureNoteUpdateRequest request)
        {
            var entity = await GetNoteForInstructorAsync(lectureId, noteId, currentUserService.UserId, track: true);

            entity.Title = request.Title.Trim();
            entity.Content = request.Content.Trim();
            entity.UpdatedAt = clock.UtcNow;
            entity.UpdatedById = currentUserService.UserId;

            await context.SaveChangesAsync();

            return Map(entity);
        }

        public async Task DeleteAsync(int lectureId, int noteId)
        {
            var entity = await GetNoteForInstructorAsync(lectureId, noteId, currentUserService.UserId, track: true);

            entity.IsActive = false;
            entity.UpdatedAt = clock.UtcNow;
            entity.UpdatedById = currentUserService.UserId;

            await context.SaveChangesAsync();
        }

        public async Task<PagedResult<LectureNoteDto>> GetForStudentAsync(int lectureId, int page, int pageSize)
        {
            var student = await UserProfileHelper.GetStudentByUserIdAsync(context, currentUserService.UserId);

            var query = context.Set<LectureNote>()
                .AsNoTracking()
                .Where(x => x.LectureId == lectureId && 
                            x.Lecture.Course.IsPublished && 
                            !x.Lecture.IsCancelled && 
                            x.Lecture.Course.Enrollments.Any(e => e.StudentId == student.Id && e.EnrollmentStatus == EnrollmentStatus.Active));

            return await query.ToPagedResultAsync(
                page,
                pageSize,
                includeTotalCount: true,
                Map,
                q => q.OrderByDescending(x => x.CreatedAt));
        }

        public async Task<LectureNoteDto> GetByIdForStudentAsync(int lectureId, int noteId)
        {
            var student = await UserProfileHelper.GetStudentByUserIdAsync(context, currentUserService.UserId);

            var entity = await context.Set<LectureNote>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == noteId && 
                                          x.LectureId == lectureId && 
                                          x.Lecture.Course.IsPublished && 
                                          !x.Lecture.IsCancelled && 
                                          x.Lecture.Course.Enrollments.Any(e => e.StudentId == student.Id && e.EnrollmentStatus == EnrollmentStatus.Active))
                ?? throw new eNote.Application.Common.Exceptions.NotFoundException(Messages.LectureNoteNotFound);

            return Map(entity);
        }

        private async Task<LectureNote> GetNoteForInstructorAsync(int lectureId, int noteId, int instructorUserId, bool track = false)
        {
            var instructor = await UserProfileHelper.GetInstructorByUserIdAsync(context, instructorUserId);
            var query = context.Set<LectureNote>().Where(x => x.Id == noteId && x.LectureId == lectureId && x.Lecture.Course.InstructorId == instructor.Id);
            return await (track ? query : query.AsNoTracking()).FirstOrDefaultAsync() ?? throw new NotFoundException(Messages.LectureNoteNotFound);
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