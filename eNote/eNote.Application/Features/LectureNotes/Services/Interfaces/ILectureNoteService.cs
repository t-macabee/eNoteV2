using eNote.Application.Common.Paging;

namespace eNote.Application.Features.LectureNotes.Services
{
    public interface ILectureNoteService
    {
        Task<PagedResult<LectureNoteDto>> GetForLectureAsync(int lectureId, int page, int pageSize);
        Task<LectureNoteDto> GetByIdForInstructorAsync(int lectureId, int noteId);
        Task<LectureNoteDto> CreateAsync(int lectureId, LectureNoteRequest request);
        Task<LectureNoteDto> UpdateAsync(int lectureId, int noteId, LectureNoteRequest request);
        Task DeleteAsync(int lectureId, int noteId);
        Task<PagedResult<LectureNoteDto>> GetForStudentAsync(int lectureId, int page, int pageSize);
        Task<LectureNoteDto> GetByIdForStudentAsync(int lectureId, int noteId);
    }
}
