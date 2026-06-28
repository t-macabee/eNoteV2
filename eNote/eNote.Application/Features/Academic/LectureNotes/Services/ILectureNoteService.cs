using eNote.Application.Common.Paging;
using eNote.Application.Features.Academic.LectureNotes;

namespace eNote.Application.Features.Academic.LectureNotes.Services;

public interface ILectureNoteService
{
    Task<PagedResult<LectureNoteDto>> GetForLectureAsync(int lectureId, LectureNoteSearchObject search, CancellationToken cancellationToken = default);
    Task<LectureNoteDto> GetByIdForInstructorAsync(int lectureId, int noteId, CancellationToken cancellationToken = default);
    Task<LectureNoteDto> CreateAsync(int lectureId, LectureNoteRequest request, CancellationToken cancellationToken = default);
    Task<LectureNoteDto> UpdateAsync(int lectureId, int noteId, LectureNoteRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int lectureId, int noteId, CancellationToken cancellationToken = default);
    Task<PagedResult<LectureNoteDto>> GetForStudentAsync(int lectureId, LectureNoteSearchObject search, CancellationToken cancellationToken = default);
    Task<LectureNoteDto> GetByIdForStudentAsync(int lectureId, int noteId, CancellationToken cancellationToken = default);
}
