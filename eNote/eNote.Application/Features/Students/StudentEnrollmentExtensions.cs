using eNote.Application.Common.Persistence;
using eNote.Domain.Entities;
using eNote.Domain.Entities.Assignments;
using eNote.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Students;

public static class StudentEnrollmentExtensions
{
    public static Task<bool> IsEnrolledInCourseAsync(this IAppDbContext context, int studentId, int courseId) =>
        context.Set<Enrollment>().AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId && e.EnrollmentStatus == EnrollmentStatus.Active);

    public static IQueryable<Lecture> ForEnrolledStudent(this IQueryable<Lecture> query, int studentId) =>
        query.Where(x => x.Course.IsPublished && x.LectureStatus != LectureStatus.Cancelled && x.Course.Enrollments.Any(e => e.StudentId == studentId && e.EnrollmentStatus == EnrollmentStatus.Active));

    public static IQueryable<LectureNote> ForEnrolledStudent(this IQueryable<LectureNote> query, int studentId) =>
        query.Where(x => x.Lecture.Course.IsPublished && x.Lecture.LectureStatus != LectureStatus.Cancelled && x.Lecture.Course.Enrollments.Any(e => e.StudentId == studentId && e.EnrollmentStatus == EnrollmentStatus.Active));

    public static IQueryable<Assignment> ForEnrolledStudent(this IQueryable<Assignment> query, int studentId) =>
        query.Where(x => x.Lecture.LectureStatus != LectureStatus.Cancelled && x.Lecture.Course.Enrollments.Any(e => e.StudentId == studentId && e.EnrollmentStatus == EnrollmentStatus.Active));
}