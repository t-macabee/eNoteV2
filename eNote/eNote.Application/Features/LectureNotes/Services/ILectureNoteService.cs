using eNote.Application.Common.Paging;
using eNote.Application.Features.LectureNotes;

namespace eNote.Application.Features.LectureNotes.Services
{
    public interface ILectureNoteService
    {
        Task<PagedResult<LectureNoteDto>> GetForLectureAsync(int lectureId, LectureNoteSearchObject search);
        Task<LectureNoteDto> GetByIdForInstructorAsync(int lectureId, int noteId);
        Task<LectureNoteDto> CreateAsync(int lectureId, LectureNoteRequest request);
        Task<LectureNoteDto> UpdateAsync(int lectureId, int noteId, LectureNoteRequest request);
        Task DeleteAsync(int lectureId, int noteId);
        Task<PagedResult<LectureNoteDto>> GetForStudentAsync(int lectureId, LectureNoteSearchObject search);
        Task<LectureNoteDto> GetByIdForStudentAsync(int lectureId, int noteId);
    }
}
