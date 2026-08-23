using eNote.Application.Common.Search;

namespace eNote.Application.Features.Academic.Assignments;

public static class AssignmentSearchExtensions
{
    public static IQueryable<Assignment> ApplySearch(this IQueryable<Assignment> query, AssignmentSearchObject search)
    {
        var dueAfter = search.DueAfter.ToUtc();
        var dueBefore = search.DueBefore.ToUtc();

        if (!string.IsNullOrWhiteSpace(search.Title))
        {
            query = query.Where(x => x.Title.Contains(search.Title!));
        }

        if (dueAfter.HasValue)
        {
            query = query.Where(x => x.DueAt >= dueAfter.Value);
        }

        if (dueBefore.HasValue)
        {
            query = query.Where(x => x.DueAt <= dueBefore.Value);
        }

        return query;
    }

    public static IQueryable<Assignment> ForEnrolledStudentById(this IQueryable<Assignment> query, int studentId, int assignmentId) =>
        query.ForEnrolledStudent(studentId).Where(x => x.Id == assignmentId);
}