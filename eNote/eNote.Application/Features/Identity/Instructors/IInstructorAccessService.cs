namespace eNote.Application.Features.Identity.Instructors;

public interface IInstructorAccessService
{
    Task<Instructor> GetInstructorAsync(int userId);
    Task<int> GetCurrentInstructorIdAsync(int appUserId);

    Task<bool> OwnsCourseAsync(int courseId, int instructorId, CancellationToken cancellationToken = default);

    Task EnsureOwnsCourseAsync(int courseId, int instructorId, CancellationToken cancellationToken = default);

    Task EnsureOwnsLectureAsync(int lectureId, int instructorId, CancellationToken cancellationToken = default);

    Task<Lecture> GetOwnedLectureAsync(int lectureId, int instructorId, bool track = false, bool includeAttendances = false, CancellationToken cancellationToken = default);

    Task<Assignment> GetOwnedAssignmentAsync(int lectureId, int assignmentId, int instructorId, bool track = false, CancellationToken cancellationToken = default);

    Task<LectureNote> GetOwnedLectureNoteAsync(int lectureId, int noteId, int instructorId, bool track = false, CancellationToken cancellationToken = default);

    IQueryable<Course> CoursesFor(int instructorId);

    IQueryable<Lecture> LecturesFor(int instructorId);

    IQueryable<Assignment> AssignmentsForLecture(int lectureId, int instructorId);

    IQueryable<LectureNote> LectureNotesForLecture(int lectureId, int instructorId);

    IQueryable<Announcement> CourseAnnouncementsFor(int courseId, int instructorId, bool track = false);
}
