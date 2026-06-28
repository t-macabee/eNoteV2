using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Academic.LectureNotes;
using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Academic.Courses;
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
    public async Task<PagedResult<LectureNoteDto>> GetForLectureAsync(int lectureId, LectureNoteSearchObject search, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);

        var query = instructorAccess.LectureNotesForLecture(lectureId, instructorId)
            .AsNoTracking()
            .ApplySearch(search);

        return await query.ToPagedResultAsync(search, mapper.Map<LectureNoteDto>, q => q.OrderByDescending(x => x.CreatedAt), cancellationToken);
    }

    public async Task<LectureNoteDto> GetByIdForInstructorAsync(int lectureId, int noteId, CancellationToken cancellationToken = default) =>
        mapper.Map<LectureNoteDto>(await GetOwnedNoteAsync(lectureId, noteId, cancellationToken: cancellationToken));

    public async Task<LectureNoteDto> CreateAsync(int lectureId, LectureNoteRequest request, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);
        await instructorAccess.EnsureOwnsLectureAsync(lectureId, instructorId, cancellationToken);

        var entity = new LectureNote(request.Title.Trim(), request.Content.Trim(), lectureId)
        {
            CreatedById = actor.UserId
        };

        context.Set<LectureNote>().Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return mapper.Map<LectureNoteDto>(entity);
    }

    public async Task<LectureNoteDto> UpdateAsync(int lectureId, int noteId, LectureNoteRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await GetOwnedNoteAsync(lectureId, noteId, track: true, cancellationToken: cancellationToken);

        entity.UpdateDetails(request.Title.Trim(), request.Content.Trim());
        entity.UpdatedById = actor.UserId;

        await context.SaveChangesAsync(cancellationToken);

        return mapper.Map<LectureNoteDto>(entity);
    }

    public async Task DeleteAsync(int lectureId, int noteId, CancellationToken cancellationToken = default)
    {
        var entity = await GetOwnedNoteAsync(lectureId, noteId, track: true, cancellationToken: cancellationToken);

        entity.SoftDelete();
        entity.UpdatedById = actor.UserId;

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<LectureNoteDto>> GetForStudentAsync(int lectureId, LectureNoteSearchObject search, CancellationToken cancellationToken = default)
    {
        var studentId = await actor.GetCurrentStudentIdAsync();

        var query = context.Set<LectureNote>()
            .AsNoTracking()
            .ForEnrolledStudent(studentId)
            .Where(x => x.LectureId == lectureId)
            .ApplySearch(search);

        return await query.ToPagedResultAsync(search, mapper.Map<LectureNoteDto>, q => q.OrderByDescending(x => x.CreatedAt), cancellationToken);
    }

    public async Task<LectureNoteDto> GetByIdForStudentAsync(int lectureId, int noteId, CancellationToken cancellationToken = default)
    {
        var studentId = await actor.GetCurrentStudentIdAsync();

        var entity = await context.Set<LectureNote>()
            .AsNoTracking()
            .ForEnrolledStudent(studentId)
            .FirstOrDefaultAsync(x => x.Id == noteId && x.LectureId == lectureId, cancellationToken)
            ?? throw new NotFoundException(Messages.LectureNoteNotFound);

        return mapper.Map<LectureNoteDto>(entity);
    }

    private async Task<LectureNote> GetOwnedNoteAsync(int lectureId, int noteId, bool track = false, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);
        return await instructorAccess.GetOwnedLectureNoteAsync(lectureId, noteId, instructorId, track, cancellationToken);
    }
}
