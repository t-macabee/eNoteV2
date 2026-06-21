using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Instructors;
using eNote.Application.Features.Students;
using eNote.Application.Features.Users.Services;
using eNote.Domain.Entities;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.LectureNotes.Services;

public sealed class LectureNoteService(
    IAppDbContext context,
    IUserContextResolver resolver,
    IInstructorAccessService instructorAccess,
    ICurrentUserService currentUserService,
    IMapper mapper) : ILectureNoteService
{
    public async Task<PagedResult<LectureNoteDto>> GetForLectureAsync(int lectureId, LectureNoteSearchObject search)
    {
        var instructor = await instructorAccess.GetInstructorAsync(currentUserService.UserId);

        var query = instructorAccess.LectureNotesForLecture(lectureId, instructor.Id)
            .AsNoTracking()
            .ApplySearch(search);

        return await query.ToPagedResultAsync(
            search.Page, search.PageSize, search.IncludeTotalCount,
            mapper.Map<LectureNoteDto>,
            q => q.OrderByDescending(x => x.CreatedAt));
    }

    public async Task<LectureNoteDto> GetByIdForInstructorAsync(int lectureId, int noteId) =>
        mapper.Map<LectureNoteDto>(await GetOwnedNoteAsync(lectureId, noteId));

    public async Task<LectureNoteDto> CreateAsync(int lectureId, LectureNoteRequest request)
    {
        var instructor = await instructorAccess.GetInstructorAsync(currentUserService.UserId);
        await instructorAccess.EnsureOwnsLectureAsync(lectureId, instructor.Id);

        var entity = new LectureNote(request.Title.Trim(), request.Content.Trim(), lectureId)
        {
            CreatedById = currentUserService.UserId
        };

        context.Set<LectureNote>().Add(entity);
        await context.SaveChangesAsync();

        return mapper.Map<LectureNoteDto>(entity);
    }

    public async Task<LectureNoteDto> UpdateAsync(int lectureId, int noteId, LectureNoteRequest request)
    {
        var entity = await GetOwnedNoteAsync(lectureId, noteId, track: true);

        entity.UpdateDetails(request.Title.Trim(), request.Content.Trim());
        entity.UpdatedById = currentUserService.UserId;

        await context.SaveChangesAsync();

        return mapper.Map<LectureNoteDto>(entity);
    }

    public async Task DeleteAsync(int lectureId, int noteId)
    {
        var entity = await GetOwnedNoteAsync(lectureId, noteId, track: true);

        entity.SoftDelete();
        entity.UpdatedById = currentUserService.UserId;

        await context.SaveChangesAsync();
    }

    public async Task<PagedResult<LectureNoteDto>> GetForStudentAsync(int lectureId, LectureNoteSearchObject search)
    {
        var student = await resolver.GetStudentAsync(currentUserService.UserId);

        var query = context.Set<LectureNote>()
            .AsNoTracking()
            .ForEnrolledStudent(student.Id)
            .Where(x => x.LectureId == lectureId)
            .ApplySearch(search);

        return await query.ToPagedResultAsync(
            search.Page, search.PageSize, search.IncludeTotalCount,
            mapper.Map<LectureNoteDto>,
            q => q.OrderByDescending(x => x.CreatedAt));
    }

    public async Task<LectureNoteDto> GetByIdForStudentAsync(int lectureId, int noteId)
    {
        var student = await resolver.GetStudentAsync(currentUserService.UserId);

        var entity = await context.Set<LectureNote>()
            .AsNoTracking()
            .ForEnrolledStudent(student.Id)
            .FirstOrDefaultAsync(x => x.Id == noteId && x.LectureId == lectureId)
            ?? throw new NotFoundException(Messages.LectureNoteNotFound);

        return mapper.Map<LectureNoteDto>(entity);
    }

    private async Task<LectureNote> GetOwnedNoteAsync(int lectureId, int noteId, bool track = false)
    {
        var instructor = await instructorAccess.GetInstructorAsync(currentUserService.UserId);
        return await instructorAccess.GetOwnedLectureNoteAsync(lectureId, noteId, instructor.Id, track);
    }
}
