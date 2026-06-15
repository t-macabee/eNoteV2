using eNote.Application.Common.Paging;

namespace eNote.Application.Features.LectureNotes.Services.Interfaces
{
    public interface ILectureNoteService
    {
        Task<PagedResult<LectureNoteDto>> GetForLectureAsync(int lectureId, int instructorUserId, int page, int pageSize);
        Task<LectureNoteDto> GetByIdForInstructorAsync(int lectureId, int noteId, int instructorUserId);
        Task<LectureNoteDto> CreateAsync(int lectureId, int instructorUserId, LectureNoteCreateRequest request);
        Task<LectureNoteDto> UpdateAsync(int lectureId, int noteId, int instructorUserId, LectureNoteUpdateRequest request);
        Task DeleteAsync(int lectureId, int noteId, int instructorUserId);
        Task<PagedResult<LectureNoteDto>> GetForStudentAsync(int lectureId, int studentUserId, int page, int pageSize);
        Task<LectureNoteDto> GetByIdForStudentAsync(int lectureId, int noteId, int studentUserId);
    }
}
