using System.Linq.Expressions;

namespace eNote.Application.Features.Academic;

public static class StudentEnrollmentExtensions
{
    public static Expression<Func<Enrollment, bool>> HasContentAccess(int studentId, DateTime utcNow) =>
        e => e.StudentId == studentId
            && e.EnrollmentStatus == EnrollmentStatus.Active
            && (e.PaidUntil >= utcNow || e.Course.Price == 0);

    public static Task<bool> IsEnrolledInCourseAsync(this IAppDbContext context, int studentId, int courseId, CancellationToken cancellationToken = default) =>
        context.Set<Enrollment>().AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId && e.EnrollmentStatus == EnrollmentStatus.Active, cancellationToken);

    public static Task<bool> IsEnrolledAndPaidAsync(this IAppDbContext context, int studentId, int courseId, DateTime utcNow, CancellationToken cancellationToken = default) =>
        context.Set<Enrollment>()
            .Where(HasContentAccess(studentId, utcNow))
            .AnyAsync(e => e.CourseId == courseId, cancellationToken);

    public static IQueryable<Lecture> ForEnrolledStudent(this IQueryable<Lecture> query, int studentId, DateTime utcNow) =>
        query.Where(x => x.Course.Enrollments.AsQueryable().Any(HasContentAccess(studentId, utcNow))
            && (x.StudentId == null || x.StudentId == studentId));

    public static IQueryable<LectureNote> ForEnrolledStudent(this IQueryable<LectureNote> query, int studentId, DateTime utcNow) =>
        query.Where(x => x.Lecture.LectureStatus != LectureStatus.Cancelled
            && x.Lecture.Course.Enrollments.AsQueryable().Any(HasContentAccess(studentId, utcNow))
            && (x.Lecture.StudentId == null || x.Lecture.StudentId == studentId));

    public static IQueryable<Assignment> ForEnrolledStudent(this IQueryable<Assignment> query, int studentId, DateTime utcNow) =>
        query.Where(x => x.Lecture.LectureStatus != LectureStatus.Cancelled
            && x.Lecture.Course.Enrollments.AsQueryable().Any(HasContentAccess(studentId, utcNow))
            && (x.Lecture.StudentId == null || x.Lecture.StudentId == studentId));
}
