using eNote.Application.Common.Search;

namespace eNote.Application.Features.Academic.Assignments;

public static class AssignmentSearchExtensions
{
    public static IQueryable<Assignment> ApplySearch(this IQueryable<Assignment> query, AssignmentSearchObject search)
    {
        var dueAfter = search.DueAfter.HasValue ? DateTime.SpecifyKind(search.DueAfter.Value, DateTimeKind.Utc) : (DateTime?)null;
        var dueBefore = search.DueBefore.HasValue ? DateTime.SpecifyKind(search.DueBefore.Value, DateTimeKind.Utc) : (DateTime?)null;

        return query
            .WhereContainsIf(search.Title, x => x.Title.Contains(search.Title!))
            .WhereEqualsIf(dueAfter, x => x.DueAt >= dueAfter!.Value)
            .WhereEqualsIf(dueBefore, x => x.DueAt <= dueBefore!.Value);
    }

    public static IQueryable<Assignment> ForEnrolledStudentById(this IQueryable<Assignment> query, int studentId, int assignmentId) =>
        query.ForEnrolledStudent(studentId).Where(x => x.Id == assignmentId);
}