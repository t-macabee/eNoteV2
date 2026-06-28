using eNote.Domain.Entities;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Academic.Courses;
using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Academic.Courses.Services;

public sealed class RankingService(IAppDbContext context, ICurrentActor actor, IStudentDisplayNameService displayNames, IInstructorAccessService instructorAccess) : IRankingService
{
    public async Task<IReadOnlyList<CourseRankingEntryDto>> GetForInstructorAsync(int courseId)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);

        if (!await instructorAccess.OwnsCourseAsync(courseId, instructorId))
        {
            throw new NotFoundException(Messages.CourseNotFound);
        }

        return await BuildRankingAsync(courseId);
    }

    public async Task<IReadOnlyList<CourseRankingEntryDto>> GetForStudentAsync(int courseId)
    {
        var studentId = await actor.GetCurrentStudentIdAsync();

        if (!await context.IsEnrolledInCourseAsync(studentId, courseId))
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

        HashSet<int> studentIds = [.. enrolledStudents.Select(s => s.Id)];

        Dictionary<int, StudentGradeStats> gradeData = await context.Set<AssignmentSubmission>()
            .AsNoTracking()
            .Where(s => s.Grade != null && s.Assignment.Lecture.CourseId == courseId && studentIds.Contains(s.StudentId))
            .GroupBy(s => s.StudentId)
            .Select(g =>
                new StudentGradeStats(g.Key,
                    g.Average(x => (double?)x.Grade),
                    g.Count()))
                .ToDictionaryAsync(x => x.StudentId);

        IReadOnlyDictionary<int, string> nameMap = await displayNames.GetStudentDisplayNamesAsync(enrolledStudents);

        List<CourseRankingEntryDto> ranked = [.. enrolledStudents
            .Where(s => gradeData.ContainsKey(s.Id))
            .Select(s =>
            {
                var gradeStats = gradeData[s.Id];

                return new RankedStudentEntry(s, gradeStats.Average, gradeStats.Count, nameMap.GetValueOrDefault(s.Id, $"Student {s.Id}"));
            })
            .OrderByDescending(x => x.Average)
            .ThenBy(x => x.Student.Id)
            .Select((x, i) => new CourseRankingEntryDto
            {
                Rank = i + 1,
                StudentId = x.Student.Id,
                StudentName = x.Name,
                AverageGrade = x.Average.HasValue ? Math.Round(x.Average!.Value, 2) : null,
                GradedSubmissions = x.Count
            })];

        return ranked;
    }

    private sealed record StudentGradeStats(int StudentId, double? Average, int Count);

    private sealed record RankedStudentEntry(Student Student, double? Average, int Count, string Name);
}
