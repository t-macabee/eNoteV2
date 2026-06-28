using eNote.Domain.Entities;

namespace eNote.Application.Features.Identity.Instructors;

public interface IInstructorAccessService
{
    Task<Instructor> GetInstructorAsync(int userId);
    Task<int> GetCurrentInstructorIdAsync(int appUserId);

    Task<bool> OwnsCourseAsync(int courseId, int instructorId);

    Task EnsureOwnsCourseAsync(int courseId, int instructorId);

    Task EnsureOwnsLectureAsync(int lectureId, int instructorId);

    Task<Lecture> GetOwnedLectureAsync(int lectureId, int instructorId, bool track = false, bool includeAttendances = false);

    Task<Assignment> GetOwnedAssignmentAsync(int lectureId, int assignmentId, int instructorId, bool track = false);

    Task<LectureNote> GetOwnedLectureNoteAsync(int lectureId, int noteId, int instructorId, bool track = false);

    IQueryable<Course> CoursesFor(int instructorId);

    IQueryable<Lecture> LecturesFor(int instructorId);

    IQueryable<Assignment> AssignmentsForLecture(int lectureId, int instructorId);

    IQueryable<LectureNote> LectureNotesForLecture(int lectureId, int instructorId);

    IQueryable<Announcement> CourseAnnouncementsFor(int courseId, int instructorId, bool track = false);
}
