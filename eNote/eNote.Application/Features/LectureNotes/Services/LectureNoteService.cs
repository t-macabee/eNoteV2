using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Users.Services;
using eNote.Domain.Entities;
using eNote.Domain.Enums;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.LectureNotes.Services
{
    public class LectureNoteService(IAppDbContext context, IUserContextResolver resolver, ICurrentUserService currentUserService, IMapper mapper) : ILectureNoteService
    {
        public async Task<PagedResult<LectureNoteDto>> GetForLectureAsync(int lectureId, LectureNoteSearchObject search)
        {
            Instructor instructor = await resolver.GetInstructorAsync(currentUserService.UserId);

            IQueryable<LectureNote> query = context.Set<LectureNote>()
                .AsNoTracking()
                .Where(x => x.LectureId == lectureId && x.Lecture.Course.InstructorId == instructor.Id);

            query = query.ApplySearch(search);

            return await query.ToPagedResultAsync(search.Page, search.PageSize, search.IncludeTotalCount, entity => mapper.Map<LectureNoteDto>(entity), q => q.OrderByDescending(x => x.CreatedAt));
        }

        public async Task<LectureNoteDto> GetByIdForInstructorAsync(int lectureId, int noteId)
        {
            LectureNote entity = await GetNoteForInstructorAsync(lectureId, noteId, currentUserService.UserId);

            return mapper.Map<LectureNoteDto>(entity);
        }

        public async Task<LectureNoteDto> CreateAsync(int lectureId, LectureNoteRequest request)
        {
            Instructor instructor = await resolver.GetInstructorAsync(currentUserService.UserId);

            _ = await context.Set<Lecture>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == lectureId && x.Course.InstructorId == instructor.Id)
                ?? throw new AuthorizationException(Messages.CourseNotOwned);

            var entity = new LectureNote(request.Title.Trim(), request.Content.Trim(), lectureId);
            entity.CreatedById = currentUserService.UserId;

            context.Set<LectureNote>().Add(entity);
            await context.SaveChangesAsync();

            return mapper.Map<LectureNoteDto>(entity);
        }

        public async Task<LectureNoteDto> UpdateAsync(int lectureId, int noteId, LectureNoteRequest request)
        {
            LectureNote entity = await GetNoteForInstructorAsync(lectureId, noteId, currentUserService.UserId, track: true);

            entity.UpdateDetails(request.Title.Trim(), request.Content.Trim());
            entity.UpdatedById = currentUserService.UserId;

            await context.SaveChangesAsync();

            return mapper.Map<LectureNoteDto>(entity);
        }

        public async Task DeleteAsync(int lectureId, int noteId)
        {
            LectureNote entity = await GetNoteForInstructorAsync(lectureId, noteId, currentUserService.UserId, track: true);

            entity.SoftDelete();
            entity.UpdatedById = currentUserService.UserId;

            await context.SaveChangesAsync();
        }

        public async Task<PagedResult<LectureNoteDto>> GetForStudentAsync(int lectureId, LectureNoteSearchObject search)
        {
            Student student = await resolver.GetStudentAsync(currentUserService.UserId);

            IQueryable<LectureNote> query = context.Set<LectureNote>()
                .AsNoTracking()
                .Where(x => x.LectureId == lectureId &&
                            x.Lecture.Course.IsPublished &&
                            x.Lecture.LectureStatus != LectureStatus.Cancelled &&
                            x.Lecture.Course.Enrollments.Any(e => e.StudentId == student.Id && e.EnrollmentStatus == EnrollmentStatus.Active));

            query = query.ApplySearch(search);

            return await query.ToPagedResultAsync(search.Page, search.PageSize, search.IncludeTotalCount, entity => mapper.Map<LectureNoteDto>(entity), q => q.OrderByDescending(x => x.CreatedAt));
        }

        public async Task<LectureNoteDto> GetByIdForStudentAsync(int lectureId, int noteId)
        {
            Student student = await resolver.GetStudentAsync(currentUserService.UserId);

            LectureNote entity = await context.Set<LectureNote>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == noteId &&
                                          x.LectureId == lectureId &&
                                          x.Lecture.Course.IsPublished &&
                                          x.Lecture.LectureStatus != LectureStatus.Cancelled &&
                                          x.Lecture.Course.Enrollments.Any(e => e.StudentId == student.Id && e.EnrollmentStatus == EnrollmentStatus.Active))
                ?? throw new NotFoundException(Messages.LectureNoteNotFound);

            return mapper.Map<LectureNoteDto>(entity);
        }

        private async Task<LectureNote> GetNoteForInstructorAsync(int lectureId, int noteId, int instructorUserId, bool track = false)
        {
            Instructor instructor = await resolver.GetInstructorAsync(instructorUserId);

            IQueryable<LectureNote> query = context.Set<LectureNote>()
                .Where(x => x.Id == noteId && x.LectureId == lectureId && x.Lecture.Course.InstructorId == instructor.Id);

            return await (track ? query : query.AsNoTracking())
                .FirstOrDefaultAsync()
                ?? throw new NotFoundException(Messages.LectureNoteNotFound);
        }

    }
}
