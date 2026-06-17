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
            var instructor = await resolver.GetInstructorAsync(currentUserService.UserId);

            var courseExists = await context.Set<Course>()
                .AnyAsync(c => c.Id == courseId && c.InstructorId == instructor.Id);

            if (!courseExists)
            {
                throw new NotFoundException(Messages.CourseNotFound);
            }

            return await BuildRankingAsync(courseId);
        }

        public async Task<IReadOnlyList<CourseRankingEntryDto>> GetForStudentAsync(int courseId)
        {
            var student = await resolver.GetStudentAsync(currentUserService.UserId);

            var isEnrolled = await context.Set<Enrollment>()
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
            var enrolledStudents = await context.Set<Enrollment>()
                .AsNoTracking()
                .Where(e => e.CourseId == courseId && e.EnrollmentStatus == EnrollmentStatus.Active)
                .Include(e => e.Student)
                .Select(e => e.Student)
                .ToListAsync();

            if (enrolledStudents.Count == 0)
            {
                return [];
            }

            var studentIds = enrolledStudents.Select(s => s.Id).ToHashSet();

            var gradeData = await context.Set<AssignmentSubmission>()
                .AsNoTracking()
                .Where(s => s.Grade != null &&
                            s.Assignment.Lecture.CourseId == courseId &&
                            studentIds.Contains(s.StudentId))
                .GroupBy(s => s.StudentId)
                .Select(g => new
                {
                    StudentId = g.Key,
                    Average = g.Average(x => (double?)x.Grade),
                    Count = g.Count()
                })
                .ToDictionaryAsync(x => x.StudentId);

            var nameMap = await resolver.GetStudentDisplayNamesAsync(enrolledStudents);

            var ranked = enrolledStudents
                .Select(s =>
                {
                    var hasGrades = gradeData.TryGetValue(s.Id, out var g);
                    return new
                    {
                        Student = s,
                        Average = hasGrades ? g!.Average : null,
                        Count = hasGrades ? g!.Count : 0,
                        Name = nameMap.GetValueOrDefault(s.Id, $"Student {s.Id}")
                    };
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
    }
}
