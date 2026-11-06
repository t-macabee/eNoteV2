namespace eNote.Application.Features.Academic.Lectures;

public static class AttendanceQueryExtensions
{
    public static IQueryable<Attendance> ShownForLecture(this IQueryable<Attendance> query) =>
        query.Where(x => x.Lecture.StudentId == null || x.StudentId == x.Lecture.StudentId);
}
