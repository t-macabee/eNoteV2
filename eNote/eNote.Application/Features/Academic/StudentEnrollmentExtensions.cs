namespace eNote.Application.Features.Academic;

// The paid predicate (e.PaidUntil >= utcNow || Course.Price == 0) is inlined four times below because EF
// cannot translate Enrollment.HasPaidAccess. Keep the four in sync with it; the comparison is on the exact
// timestamp by design (see Enrollment.cs:45).
public static class StudentEnrollmentExtensions
{
    public static Task<bool> IsEnrolledInCourseAsync(this IAppDbContext context, int studentId, int courseId, CancellationToken cancellationToken = default) =>
        context.Set<Enrollment>().AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId && e.EnrollmentStatus == EnrollmentStatus.Active, cancellationToken);

    public static Task<bool> IsEnrolledAndPaidAsync(this IAppDbContext context, int studentId, int courseId, DateTime utcNow, CancellationToken cancellationToken = default) =>
        context.Set<Enrollment>().AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId && e.EnrollmentStatus == EnrollmentStatus.Active && (e.PaidUntil >= utcNow || e.Course.Price == 0), cancellationToken);

    public static IQueryable<Lecture> ForEnrolledStudent(this IQueryable<Lecture> query, int studentId, DateTime utcNow) =>
        query.Where(x => x.Course.IsPublished && x.LectureStatus != LectureStatus.Cancelled && x.Course.Enrollments.Any(e => e.StudentId == studentId && e.EnrollmentStatus == EnrollmentStatus.Active && (e.PaidUntil >= utcNow || x.Course.Price == 0)));

    public static IQueryable<LectureNote> ForEnrolledStudent(this IQueryable<LectureNote> query, int studentId, DateTime utcNow) =>
        query.Where(x => x.Lecture.Course.IsPublished && x.Lecture.LectureStatus != LectureStatus.Cancelled && x.Lecture.Course.Enrollments.Any(e => e.StudentId == studentId && e.EnrollmentStatus == EnrollmentStatus.Active && (e.PaidUntil >= utcNow || x.Lecture.Course.Price == 0)));

    public static IQueryable<Assignment> ForEnrolledStudent(this IQueryable<Assignment> query, int studentId, DateTime utcNow) =>
        query.Where(x => x.Lecture.Course.IsPublished && x.Lecture.LectureStatus != LectureStatus.Cancelled && x.Lecture.Course.Enrollments.Any(e => e.StudentId == studentId && e.EnrollmentStatus == EnrollmentStatus.Active && (e.PaidUntil >= utcNow || x.Lecture.Course.Price == 0)));
}
