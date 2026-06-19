using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Users.Services;
using eNote.Domain.Entities;
using eNote.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Courses.Services
{
    public class RankingService(IAppDbContext context, IUserContextResolver resolver, ICurrentUserService currentUserService) : IRankingService
    {
        public async Task<IReadOnlyList<CourseRankingEntryDto>> GetForInstructorAsync(int courseId)
        {
            Instructor instructor = await resolver.GetInstructorAsync(currentUserService.UserId);

            bool courseExists = await context.Set<Course>()
                .AnyAsync(c => c.Id == courseId && c.InstructorId == instructor.Id);

            if (!courseExists)
            {
                throw new NotFoundException(Messages.CourseNotFound);
            }

            return await BuildRankingAsync(courseId);
        }

        public async Task<IReadOnlyList<CourseRankingEntryDto>> GetForStudentAsync(int courseId)
        {
            Student student = await resolver.GetStudentAsync(currentUserService.UserId);

            bool isEnrolled = await context.Set<Enrollment>()
                .AnyAsync(e => e.CourseId == courseId &&
                               e.StudentId == student.Id &&
                               e.EnrollmentStatus == EnrollmentStatus.Active);

            if (!isEnrolled)
            {
                throw new AuthorizationException(Messages.StudentNotEnrolled);
            }

            return await BuildRankingAsync(courseId);
        }

        private async Task<IReadOnlyList<CourseRankingEntryDto>> BuildRankingAsync(int courseId)
        {
            List<Student> enrolledStudents = await context.Set<Enrollment>()
                .AsNoTracking()
                .Where(e => e.CourseId == courseId && e.EnrollmentStatus == EnrollmentStatus.Active)
                .Include(e => e.Student)
                .Select(e => e.Student)
                .ToListAsync();

            if (enrolledStudents.Count == 0)
            {
                return [];
            }

            HashSet<int> studentIds = enrolledStudents.Select(s => s.Id).ToHashSet();

            Dictionary<int, StudentGradeStats> gradeData = await context.Set<AssignmentSubmission>()
                .AsNoTracking()
                .Where(s => s.Grade != null &&
                            s.Assignment.Lecture.CourseId == courseId &&
                            studentIds.Contains(s.StudentId))
                .GroupBy(s => s.StudentId)
                .Select(g => new StudentGradeStats(
                    g.Key,
                    g.Average(x => (double?)x.Grade),
                    g.Count()))
                .ToDictionaryAsync(x => x.StudentId);

            IReadOnlyDictionary<int, string> nameMap = await resolver.GetStudentDisplayNamesAsync(enrolledStudents);

            List<CourseRankingEntryDto> ranked = enrolledStudents
                .Select(s =>
                {
                    bool hasGrades = gradeData.TryGetValue(s.Id, out StudentGradeStats? gradeStats);
                    return new RankedStudentEntry(
                        s,
                        hasGrades ? gradeStats!.Average : null,
                        hasGrades ? gradeStats!.Count : 0,
                        nameMap.GetValueOrDefault(s.Id, $"Student {s.Id}"));
                })
                .OrderByDescending(x => x.Average)
                .ThenBy(x => x.Student.Id)
                .Select((x, i) => new CourseRankingEntryDto
                {
                    Rank = i + 1,
                    StudentId = x.Student.Id,
                    StudentName = x.Name,
                    AverageGrade = x.Average.HasValue ? Math.Round(x.Average.Value, 2) : null,
                    GradedSubmissions = x.Count
                })
                .ToList();

            return ranked;
        }

        private sealed record StudentGradeStats(int StudentId, double? Average, int Count);

        private sealed record RankedStudentEntry(Student Student, double? Average, int Count, string Name);
    }
}
