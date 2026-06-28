using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Academic.LectureNotes;
using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Students;
using eNote.Domain.Entities;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Academic.LectureNotes.Services;

public sealed class LectureNoteService(
    IAppDbContext context,
    ICurrentActor actor,
    IInstructorAccessService instructorAccess,
    IMapper mapper) : ILectureNoteService
{
    public async Task<PagedResult<LectureNoteDto>> GetForLectureAsync(int lectureId, LectureNoteSearchObject search)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);

        var query = instructorAccess.LectureNotesForLecture(lectureId, instructorId)
            .AsNoTracking()
            .ApplySearch(search);

        return await query.ToPagedResultAsync(search, mapper.Map<LectureNoteDto>, q => q.OrderByDescending(x => x.CreatedAt));
    }

    public async Task<LectureNoteDto> GetByIdForInstructorAsync(int lectureId, int noteId) =>
        mapper.Map<LectureNoteDto>(await GetOwnedNoteAsync(lectureId, noteId));

    public async Task<LectureNoteDto> CreateAsync(int lectureId, LectureNoteRequest request)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);
        await instructorAccess.EnsureOwnsLectureAsync(lectureId, instructorId);

        var entity = new LectureNote(request.Title.Trim(), request.Content.Trim(), lectureId)
        {
            CreatedById = actor.UserId
        };

        context.Set<LectureNote>().Add(entity);
        await context.SaveChangesAsync();

        return mapper.Map<LectureNoteDto>(entity);
    }

    public async Task<LectureNoteDto> UpdateAsync(int lectureId, int noteId, LectureNoteRequest request)
    {
        var entity = await GetOwnedNoteAsync(lectureId, noteId, track: true);

        entity.UpdateDetails(request.Title.Trim(), request.Content.Trim());
        entity.UpdatedById = actor.UserId;

        await context.SaveChangesAsync();

        return mapper.Map<LectureNoteDto>(entity);
    }

    public async Task DeleteAsync(int lectureId, int noteId)
    {
        var entity = await GetOwnedNoteAsync(lectureId, noteId, track: true);

        entity.SoftDelete();
        entity.UpdatedById = actor.UserId;

        await context.SaveChangesAsync();
    }

    public async Task<PagedResult<LectureNoteDto>> GetForStudentAsync(int lectureId, LectureNoteSearchObject search)
    {
        var studentId = await actor.GetCurrentStudentIdAsync();

        var query = context.Set<LectureNote>()
            .AsNoTracking()
            .ForEnrolledStudent(studentId)
            .Where(x => x.LectureId == lectureId)
            .ApplySearch(search);

        return await query.ToPagedResultAsync(search, mapper.Map<LectureNoteDto>, q => q.OrderByDescending(x => x.CreatedAt));
    }

    public async Task<LectureNoteDto> GetByIdForStudentAsync(int lectureId, int noteId)
    {
        var studentId = await actor.GetCurrentStudentIdAsync();

        var entity = await context.Set<LectureNote>()
            .AsNoTracking()
            .ForEnrolledStudent(studentId)
            .FirstOrDefaultAsync(x => x.Id == noteId && x.LectureId == lectureId)
            ?? throw new NotFoundException(Messages.LectureNoteNotFound);

        return mapper.Map<LectureNoteDto>(entity);
    }

    private async Task<LectureNote> GetOwnedNoteAsync(int lectureId, int noteId, bool track = false)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);
        return await instructorAccess.GetOwnedLectureNoteAsync(lectureId, noteId, instructorId, track);
    }
}
